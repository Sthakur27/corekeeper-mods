import copy
import contextlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('perf_compare', Path(__file__).with_name('compare.py'))
compare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compare)


def capture(count=1000, p95=10, p99=12):
    return {'schema': 1, 'recorderVersion': '0.1.0', 'gameVersion': '1.3', 'unityVersion': '6000',
            'cpu': 'cpu', 'gpu': 'gpu', 'width': 1920, 'height': 1080, 'vSyncCount': 0, 'targetFrameRate': -1,
            'sampleCapacityReached': False, 'droppedScopes': 0, 'reason': 'duration', 'buildIdentity': 'one',
            'durationSeconds': 63, 'warmupSeconds': 3, 'sampledSeconds': 60,
            'label': 'route', 'mods': [{'name': 'A', 'configFingerprint': 'abc'}], 'featureSwitches': [], 'scopes': [],
            'frames': {'count': count, 'meanMs': 8, 'medianMs': 8, 'p95Ms': p95, 'p99Ms': p99, 'maxMs': 20,
                       'over16_67ms': count // 10, 'over33_33ms': 0, 'over50ms': 0, 'over100ms': 0}}


class ComparisonTests(unittest.TestCase):
    def test_hitch_summary_only_for_new_captures(self):
        old = compare.compare([capture()] * 3, [capture()] * 3)
        self.assertNotIn('hitches', old['arms']['baseline'])
        new = capture()
        new['hitches'] = [{'seconds': 1, 'ms': 80, 'gc': True, 'topScope': 'A.cs:3.OnUpdate', 'topScopeMs': 70},
                          {'seconds': 2, 'ms': 40, 'gc': False, 'topScope': '', 'topScopeMs': 0}]
        new['hitchesWithGc'] = 1
        new['cpuMainMs'] = {'count': 10, 'mean': 5, 'median': 5, 'p95': 6, 'p99': 7, 'max': 9}
        result = compare.compare([new] * 3, [capture()] * 3)
        hitches = result['arms']['baseline']['hitches']
        self.assertEqual(hitches['medianPerRun'], 2)
        self.assertEqual(hitches['medianWithGcPerRun'], 1)
        self.assertEqual(hitches['topScopeCounts'], {'A.cs:3.OnUpdate': 3})
        self.assertEqual(hitches['cpuMainMsP95'], 6)
        self.assertNotIn('gpuMsP95', hitches)
        self.assertIn('baseline hitches', compare.render(result))

    def test_live_short_capture_rejected_despite_frame_count(self):
        short = capture(484)
        short['sampledSeconds'] = 2.74
        result = compare.compare([short] * 3, [capture()] * 3)
        self.assertEqual(result['status'], 'invalid')
        self.assertTrue(any('20 seconds' in issue for issue in result['issues']))

    def test_timed_capture_requires_eighty_percent_coverage(self):
        short = capture(); short['sampledSeconds'] = 47.99
        self.assertEqual(compare.compare([short], [capture()])['status'], 'invalid')
        short['sampledSeconds'] = 48
        self.assertEqual(compare.compare([short], [capture()])['status'], 'provisional')

    def test_manual_capture_requires_twenty_seconds_not_eighty_percent(self):
        manual = capture(); manual['reason'] = 'manual'; manual['sampledSeconds'] = 20
        self.assertEqual(compare.compare([manual], [capture()])['status'], 'provisional')
        manual['sampledSeconds'] = 19.99
        self.assertEqual(compare.compare([manual], [capture()])['status'], 'invalid')

    def test_missing_and_nonfinite_sampled_seconds_rejected(self):
        for value in (None, float('nan'), float('inf'), -1, True):
            with self.subTest(value=value):
                report = capture(); report['sampledSeconds'] = value
                self.assertEqual(compare.compare([report], [capture()])['status'], 'invalid')
        report = capture(); del report['sampledSeconds']
        self.assertEqual(compare.compare([report], [capture()])['status'], 'invalid')

    def test_invalid_duration_and_warmup_rejected(self):
        for key in ('durationSeconds', 'warmupSeconds'):
            for value in (None, 0, -1, float('nan'), float('inf'), True):
                with self.subTest(key=key, value=value):
                    report = capture(); report[key] = value
                    self.assertEqual(compare.compare([report], [capture()])['status'], 'invalid')
        report = capture(); report['durationSeconds'] = report['warmupSeconds']
        self.assertEqual(compare.compare([report], [capture()])['status'], 'invalid')
        report = capture(); report['sampledSeconds'] = 100
        self.assertEqual(compare.compare([report], [capture()])['status'], 'invalid')

    def test_three_runs_and_guard(self):
        result = compare.compare([capture() for _ in range(3)], [capture(p95=12) for _ in range(3)])
        self.assertEqual(result['status'], 'investigate')
        self.assertEqual(result['guardsExceeded'], ['p95Ms'])

    def test_fewer_runs_are_provisional_even_with_large_change(self):
        self.assertEqual(compare.compare([capture()], [capture(p95=100)])['status'], 'provisional')

    def test_p99_guard_independent_of_p95(self):
        result = compare.compare([capture()] * 3, [capture(p99=15)] * 3)
        self.assertEqual(result['guardsExceeded'], ['p99Ms'])

    def test_differing_lengths_normalize_stutters_per_run(self):
        result = compare.compare([capture(1000), capture(2000), capture(3000)], [capture(4000), capture(5000), capture(6000)])
        self.assertEqual(result['stuttersPer1000Frames']['over16_67ms']['delta'], 0)

    def test_run_weight_is_equal_not_frame_weight(self):
        result = compare.compare([capture(300, p95=10), capture(300, p95=10), capture(100000, p95=100)], [capture() for _ in range(3)])
        self.assertEqual(result['metrics']['p95Ms']['baseline'], 10)

    def test_bad_metadata_rejected(self):
        for key, value in [('schema', 2), ('gameVersion', 'other'), ('unityVersion', 'other'), ('cpu', 'other'), ('gpu', 'other'),
                           ('width', 100), ('height', 100), ('vSyncCount', 1), ('targetFrameRate', 60), ('recorderVersion', 'other')]:
            with self.subTest(key=key):
                modified = capture(); modified[key] = value
                self.assertEqual(compare.compare([capture()] * 3, [modified] * 3)['status'], 'invalid')

    def test_bad_mod_set_rejected(self):
        modified = capture(); modified['mods'][0]['name'] = 'B'
        self.assertEqual(compare.compare([capture()], [modified])['status'], 'invalid')

    def test_missing_quality_and_truncation_rejected(self):
        for key, value in [('sampleCapacityReached', True), ('droppedScopes', 1)]:
            modified = capture(); modified[key] = value
            self.assertEqual(compare.compare([capture()], [modified])['status'], 'invalid')
        self.assertEqual(compare.compare([capture(299)], [capture()])['status'], 'invalid')

    def test_allowed_identity_changes_listed(self):
        modified = capture(); modified['buildIdentity'] = 'two'; modified['mods'][0]['configFingerprint'] = 'def'; modified['label'] = 'other'
        result = compare.compare([capture()] * 3, [modified] * 3)
        self.assertEqual(result['status'], 'no-guard-crossing')
        self.assertEqual(set(result['identityDifferences']), {'buildIdentity', 'configFingerprints'})

    def test_zero_baseline_json_safe(self):
        result = compare.compare([capture(p95=0, p99=0)] * 3, [capture(p95=4, p99=4)] * 3)
        self.assertIsNone(result['metrics']['p95Ms']['percent'])
        json.dumps(result, allow_nan=False)
        self.assertEqual(result['status'], 'investigate')

    def test_both_guard_conditions_required_and_strict(self):
        result = compare.compare([capture(p95=10)] * 3, [capture(p95=11)] * 3)
        self.assertEqual(result['guardsExceeded'], [])
        result = compare.compare([capture(p95=100)] * 3, [capture(p95=105)] * 3)
        self.assertEqual(result['guardsExceeded'], [])

    def test_nested_scopes_never_summed(self):
        report = capture()
        report['scopes'] = [dict(name=name, calls=5, inclusiveMs=ms, maxMs=ms, allocatedBytes=10, examined=0, changed=0)
                            for name, ms in [('outer', 100), ('inner', 60)]]
        result = compare.compare([report] * 3, [copy.deepcopy(report)] * 3)
        scopes = result['arms']['baseline']['scopes']
        self.assertEqual(scopes['outer']['medianPerRun']['inclusiveMs'], 100)
        self.assertEqual(scopes['inner']['medianPerRun']['inclusiveMs'], 60)
        self.assertNotIn('total', scopes)
        self.assertIn('NEVER sum', compare.render(result))

    def test_empty_scopes_are_explicitly_unobserved(self):
        self.assertIn('No scope coverage', compare.render(compare.compare([capture()], [capture()])))

    def test_missing_scope_is_not_imputed_as_zero(self):
        observed = capture()
        observed['scopes'] = [dict(name='once', calls=5, inclusiveMs=10, maxMs=3, allocatedBytes=2, examined=0, changed=0)]
        result = compare.compare([observed, capture(), capture()], [capture()] * 3)
        scope = result['arms']['baseline']['scopes']['once']
        self.assertEqual(scope['runsObserved'], 1)
        self.assertEqual(scope['medianPerRun']['inclusiveMs'], 10)

    def test_cli_json_and_duplicate_rejection(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for arm in ('b', 'c'):
                for i in range(3):
                    (root / f'{arm}{i}.json').write_text(json.dumps(capture()))
            output = root / 'result.json'
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(compare.main(['--baseline', str(root / 'b*.json'), '--candidate', str(root / 'c*.json'), '--json', str(output)]), 0)
            self.assertEqual(json.loads(output.read_text())['status'], 'no-guard-crossing')
            with self.assertRaises(ValueError):
                compare.load_paths([str(root / 'b0.json')] * 3)


if __name__ == '__main__':
    unittest.main()
