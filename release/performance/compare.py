"""Compare repeated ModDiagnostics captures without pooling frames or nested scopes."""
import argparse
import glob
import json
import math
from pathlib import Path
import statistics

ENVIRONMENT = ('schema', 'recorderVersion', 'gameVersion', 'unityVersion', 'cpu', 'gpu',
               'width', 'height', 'vSyncCount', 'targetFrameRate')
FRAME_METRICS = ('meanMs', 'medianMs', 'p95Ms', 'p99Ms', 'maxMs')
STUTTERS = ('over16_67ms', 'over33_33ms', 'over50ms', 'over100ms')
SCOPE_METRICS = ('calls', 'inclusiveMs', 'allocatedBytes', 'examined', 'changed')


def numeric(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def validate(report):
    errors = []
    if not isinstance(report, dict):
        return ['report must be an object']
    for key in ENVIRONMENT:
        if key not in report or report[key] is None or report[key] == '':
            errors.append('missing metadata: ' + key)
    for key in ('recorderVersion', 'gameVersion', 'unityVersion', 'cpu', 'gpu'):
        if not isinstance(report.get(key), str):
            errors.append('invalid metadata type: ' + key)
    if report.get('schema') != 1:
        errors.append('unsupported schema (expected 1)')
    for key in ('width', 'height'):
        if not numeric(report.get(key)) or report[key] <= 0:
            errors.append('invalid metadata: ' + key)
    for key in ('vSyncCount', 'targetFrameRate'):
        if not numeric(report.get(key)):
            errors.append('invalid metadata: ' + key)
    if report.get('sampleCapacityReached') is not False or report.get('reason') == 'sample-capacity':
        errors.append('truncated samples or missing capacity flag')
    if report.get('droppedScopes') != 0:
        errors.append('dropped scopes or missing scope-drop count')
    duration = report.get('durationSeconds')
    warmup = report.get('warmupSeconds')
    sampled = report.get('sampledSeconds')
    valid_duration = numeric(duration) and duration > 0
    valid_warmup = numeric(warmup) and warmup > 0
    valid_interval = valid_duration and valid_warmup and duration > warmup
    if not valid_duration:
        errors.append('durationSeconds must be finite and positive')
    if not valid_warmup:
        errors.append('warmupSeconds must be finite and positive')
    if valid_duration and valid_warmup and not valid_interval:
        errors.append('durationSeconds must exceed warmupSeconds')
    if not numeric(sampled) or sampled < 20:
        errors.append('sampledSeconds must be finite and at least 20 seconds of eligible gameplay')
    elif valid_duration and sampled > duration:
        errors.append('sampledSeconds exceeds total durationSeconds')
    if (report.get('reason') == 'duration' and valid_interval and numeric(sampled)
            and sampled < 0.8 * (duration - warmup)):
        errors.append('eligible gameplay covers less than 80% of the timed interval')
    frames = report.get('frames')
    if not isinstance(frames, dict):
        return errors + ['missing frame summary']
    count = frames.get('count')
    if not isinstance(count, int) or isinstance(count, bool) or count < 300:
        errors.append('fewer than 300 valid frames or invalid frame count')
    for key in FRAME_METRICS:
        if not numeric(frames.get(key)) or frames[key] < 0:
            errors.append('invalid frame metric: ' + key)
    for key in STUTTERS:
        if not isinstance(frames.get(key), int) or frames[key] < 0 or not numeric(count) or frames[key] > count:
            errors.append('invalid stutter count: ' + key)
    mods = report.get('mods')
    if not isinstance(mods, list) or any(not isinstance(m, dict) or not isinstance(m.get('name'), str) or not m['name'] for m in mods):
        errors.append('invalid mod names')
    elif len({m['name'] for m in mods}) != len(mods):
        errors.append('duplicate mod names')
    else:
        for mod in mods:
            if mod.get('configReadError'):
                errors.append('config read failed: ' + mod['name'])
    scopes = report.get('scopes')
    if not isinstance(scopes, list):
        errors.append('missing scope list')
    else:
        names = set()
        for scope in scopes:
            if not isinstance(scope, dict) or not isinstance(scope.get('name'), str) or not scope['name']:
                errors.append('invalid scope name')
                continue
            if scope['name'] in names:
                errors.append('duplicate scope name: ' + scope['name'])
            names.add(scope['name'])
            for key in SCOPE_METRICS + ('maxMs',):
                if not numeric(scope.get(key)) or scope[key] < 0:
                    errors.append('invalid scope metric: ' + scope['name'] + '.' + key)
    return errors


def delta(before, after):
    return {'baseline': before, 'candidate': after, 'delta': after - before,
            'percent': (after / before - 1) * 100 if before else (0.0 if after == 0 else None)}


def arm_summary(runs):
    result = {'runs': len(runs), 'framesPerRun': [r['frames']['count'] for r in runs],
              'sampledSecondsPerRun': [r['sampledSeconds'] for r in runs]}
    result['frameMetrics'] = {key: statistics.median(r['frames'][key] for r in runs) for key in FRAME_METRICS}
    result['stuttersPer1000Frames'] = {
        key: statistics.median(r['frames'][key] * 1000 / r['frames']['count'] for r in runs) for key in STUTTERS}
    # Recorder 0.2+ fields; older captures simply omit this section.
    if all(isinstance(r.get('hitches'), list) for r in runs):
        tops = {}
        for r in runs:
            for h in r['hitches']:
                if h.get('topScope'):
                    tops[h['topScope']] = tops.get(h['topScope'], 0) + 1
        result['hitches'] = {
            'medianPerRun': statistics.median(len(r['hitches']) for r in runs),
            'medianWithGcPerRun': statistics.median(r.get('hitchesWithGc', 0) for r in runs),
            'topScopeCounts': dict(sorted(tops.items(), key=lambda kv: -kv[1]))}
        for key in ('cpuMainMs', 'gpuMs', 'allocatedKbPerFrame'):
            series = [r[key] for r in runs if isinstance(r.get(key), dict)]
            if len(series) == len(runs):
                result['hitches'][key + 'P95'] = statistics.median(s['p95'] for s in series)
    names = sorted({s['name'] for r in runs for s in r['scopes']})
    result['scopes'] = {}
    for name in names:
        observed = [(r, s) for r in runs for s in r['scopes'] if s['name'] == name]
        result['scopes'][name] = {
            'runsObserved': len(observed),
            'medianPerRun': {k: statistics.median(s[k] for _, s in observed) for k in SCOPE_METRICS + ('maxMs',)},
            'medianPer1000Frames': {k: statistics.median(s[k] * 1000 / r['frames']['count'] for r, s in observed) for k in SCOPE_METRICS}}
    return result


def compare(baseline, candidate, p95_percent=10, p95_ms=1, p99_percent=15, p99_ms=2):
    result = {'status': 'invalid', 'issues': [], 'identityDifferences': {}, 'arms': {}, 'metrics': {}, 'stuttersPer1000Frames': {},
              'guards': {'p95Percent': p95_percent, 'p95Ms': p95_ms, 'p99Percent': p99_percent, 'p99Ms': p99_ms},
              'limitations': [
                  'Medians of run summaries, not pooled frames; each run has equal weight. Max is median of run maxima.',
                  'Scopes are inclusive main-thread elapsed time including waits; NEVER sum nested scope timing or allocations.',
                  'Instrumentation changes execution and allocation costs. Compare recorder-only versus instrumented runs; bookkeeping is not total overhead.',
                  'Empty or missing scopes mean unobserved coverage, not zero cost. Scope medians use only runs where that scope is present.',
                  'Changed builds/configs are allowed but can confound attribution. Keep the route, workload, and recording length comparable.',
                  'Practical guards identify investigations, not statistical proof or a release pass/fail.']}
    all_runs = baseline + candidate
    for arm, runs in (('baseline', baseline), ('candidate', candidate)):
        if not runs:
            result['issues'].append(arm + ': no captures')
        for i, run in enumerate(runs):
            result['issues'].extend(f'{arm}[{i + 1}]: {e}' for e in validate(run))
    if result['issues']:
        return result
    for key in ENVIRONMENT:
        if any(r[key] != all_runs[0][key] for r in all_runs):
            result['issues'].append('mismatched metadata: ' + key)
    names = sorted(m['name'] for m in all_runs[0]['mods'])
    if any(sorted(m['name'] for m in r['mods']) != names for r in all_runs):
        result['issues'].append('mismatched mod names')
    if result['issues']:
        return result
    identities = {
        'buildIdentity': lambda r: r.get('buildIdentity'),
        'configFingerprints': lambda r: {m['name']: m.get('configFingerprint') for m in r['mods']},
        'featureSwitches': lambda r: sorted(r.get('featureSwitches', [])),
    }
    for key, getter in identities.items():
        values = [getter(r) for r in all_runs]
        if any(v != values[0] for v in values):
            result['identityDifferences'][key] = {'baseline': [getter(r) for r in baseline], 'candidate': [getter(r) for r in candidate]}
    result['arms'] = {'baseline': arm_summary(baseline), 'candidate': arm_summary(candidate)}
    for key in FRAME_METRICS:
        result['metrics'][key] = delta(result['arms']['baseline']['frameMetrics'][key], result['arms']['candidate']['frameMetrics'][key])
    for key in STUTTERS:
        result['stuttersPer1000Frames'][key] = delta(result['arms']['baseline']['stuttersPer1000Frames'][key], result['arms']['candidate']['stuttersPer1000Frames'][key])
    exceeded = []
    for key, percent, absolute in (('p95Ms', p95_percent, p95_ms), ('p99Ms', p99_percent, p99_ms)):
        metric = result['metrics'][key]
        # A positive candidate over a zero baseline exceeds any finite relative guard;
        # retain null percentage instead of serializing Infinity or dividing by zero.
        relative = ((metric['percent'] is None and metric['candidate'] > 0)
                    or (metric['percent'] is not None and metric['percent'] > percent))
        if relative and metric['delta'] > absolute:
            exceeded.append(key)
    result['guardsExceeded'] = exceeded
    if min(len(baseline), len(candidate)) < 3:
        result['status'] = 'provisional'
        result['message'] = 'Need at least three valid captures per arm before a verdict; observed guard crossings are provisional.'
    elif exceeded:
        result['status'] = 'investigate'
        result['message'] = 'Practical guard crossed: ' + ', '.join(exceeded) + '. Investigate and repeat; regression is not proven.'
    else:
        result['status'] = 'no-guard-crossing'
        result['message'] = 'No practical guard crossed. This is not proof of no regression or a release pass.'
    return result


def render(result):
    lines = ['Status: ' + result['status'], result.get('message', '')]
    lines.extend('Issue: ' + issue for issue in result['issues'])
    for key, diff in result['identityDifferences'].items():
        lines.append('Allowed identity difference ' + key + ': ' + json.dumps(diff, sort_keys=True))
    for arm, summary in result['arms'].items():
        lines.append(f"{arm}: {summary['runs']} captures; valid frames {summary['framesPerRun']}; eligible seconds {summary['sampledSecondsPerRun']}")
    for category in ('metrics', 'stuttersPer1000Frames'):
        for key, metric in result[category].items():
            percent = 'undefined (zero baseline)' if metric['percent'] is None else f"{metric['percent']:+.2f}%"
            unit = 'ms' if category == 'metrics' else '/1000 frames'
            lines.append(f"{key}: {metric['baseline']:.3f} -> {metric['candidate']:.3f} {unit}; delta {metric['delta']:+.3f}, {percent}")
    for arm, summary in result['arms'].items():
        if 'hitches' in summary:
            lines.append(arm + ' hitches (>33 ms): ' + json.dumps(summary['hitches'], sort_keys=True))
    for arm, summary in result['arms'].items():
        lines.append(arm + ' scopes (individual inclusive values; never summed):')
        if not summary['scopes']:
            lines.append('  No scope coverage recorded; frame metrics remain usable.')
        for name, scope in summary['scopes'].items():
            lines.append(f"  {name}: present {scope['runsObserved']}/{summary['runs']} runs; median/run {json.dumps(scope['medianPerRun'], sort_keys=True)}; median/1000 frames {json.dumps(scope['medianPer1000Frames'], sort_keys=True)}")
    lines.extend('Note: ' + item for item in result['limitations'])
    return '\n'.join(line for line in lines if line)


def load_paths(patterns):
    paths = []
    for pattern in patterns:
        matches = sorted(glob.glob(pattern, recursive=True))
        if not matches:
            raise ValueError('no capture matches: ' + pattern)
        for match in matches:
            path = Path(match).resolve()
            if path in paths:
                raise ValueError('duplicate capture cannot count as repeated run: ' + str(path))
            paths.append(path)
    return paths, [json.loads(p.read_text(encoding='utf-8-sig')) for p in paths]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', action='append', required=True, help='JSON path/glob, repeatable')
    parser.add_argument('--candidate', action='append', required=True, help='JSON path/glob, repeatable')
    parser.add_argument('--json', type=Path, help='also write structured comparison here')
    for key, default in (('p95-percent', 10), ('p95-ms', 1), ('p99-percent', 15), ('p99-ms', 2)):
        parser.add_argument('--' + key, type=float, default=default)
    args = parser.parse_args(argv)
    thresholds = (args.p95_percent, args.p95_ms, args.p99_percent, args.p99_ms)
    if any(not math.isfinite(x) or x < 0 for x in thresholds):
        parser.error('guards must be finite nonnegative numbers')
    try:
        bp, baseline = load_paths(args.baseline)
        cp, candidate = load_paths(args.candidate)
        if set(bp) & set(cp):
            raise ValueError('the same capture appears in both arms')
        if args.json and args.json.resolve() in set(bp + cp):
            raise ValueError('JSON output must not overwrite an input capture')
    except (OSError, ValueError) as error:
        parser.error(str(error))
    result = compare(baseline, candidate, *thresholds)
    result['inputPaths'] = {'baseline': list(map(str, bp)), 'candidate': list(map(str, cp))}
    print(render(result))
    if args.json:
        args.json.write_text(json.dumps(result, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    return 2 if result['status'] == 'invalid' else 0


if __name__ == '__main__':
    raise SystemExit(main())
