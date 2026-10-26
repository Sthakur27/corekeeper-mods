"""Stage an isolated diagnostic mod pair; never install or edit input sources.

Usage: python release/performance/stage_diagnostics.py --source GoldenChance
       --output C:/scratch/golden-profile [--no-instrument]
Output contains <manifest-name>/, ModDiagnostics/, and stage-metadata.json.
Wraps parameterless void lifecycle methods, managed ISystem OnUpdate(ref SystemState)
and static Harmony Prefix/Postfix methods in HarmonyPatch files. Skipped coverage is
explicit; this is not a general C# parser or complete CPU profiler.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re


TARGETS = r"(?:Update|OnUpdate|LateUpdate|OnGUI|Prefix|Postfix)"


def lexical_mask(text):
    """Replace comments/string/char contents with spaces, preserving offsets."""
    out = list(text)
    n = len(text)

    def blank(a, b):
        for k in range(a, b):
            if out[k] not in '\r\n':
                out[k] = ' '

    def token(i):
        if text.startswith('//', i):
            end = text.find('\n', i)
            return n if end < 0 else end
        if text.startswith('/*', i):
            end = text.find('*/', i + 2)
            if end < 0:
                raise ValueError('unterminated comment')
            return end + 2
        m = re.match(r'(?:\$@|@\$|@|\$)?"|\x27', text[i:])
        if not m:
            return None
        prefix = m.group()
        if text.startswith('"""', i) or text.startswith('$"""', i):
            raise ValueError('raw strings unsupported')
        quote = prefix[-1]
        verbatim = '@' in prefix
        interpolated = '$' in prefix
        j = i + len(prefix)
        while j < n:
            if text[j] == quote:
                if verbatim and text.startswith('""', j):
                    j += 2
                    continue
                return j + 1
            if text[j] == '\\' and not verbatim:
                j += 2
                continue
            if interpolated and text[j] == '{':
                if text.startswith('{{', j):
                    j += 2
                    continue
                depth = 1
                j += 1
                while j < n and depth:
                    end = token(j)
                    if end is not None:
                        j = end
                        continue
                    if text[j] == '{':
                        depth += 1
                    elif text[j] == '}':
                        depth -= 1
                    j += 1
                if depth:
                    raise ValueError('unterminated interpolation')
                continue
            j += 1
        raise ValueError('unterminated string/char')

    i = 0
    while i < n:
        end = token(i)
        if end is not None:
            blank(i, end)
            i = end
        else:
            i += 1
    return ''.join(out)


def instrument(text, relative):
    done, skipped = [], []

    def skip(reason, line=None, method=None):
        skipped.append(dict(file=relative, line=line, method=method, reason=reason))

    if 'generated' in relative.lower().split('/') or relative.lower().endswith('.g.cs'):
        skip('generated file')
        return text, done, skipped
    try:
        mask = lexical_mask(text)
    except ValueError as exc:
        skip(str(exc))
        return text, done, skipped
    if re.search(r'\b(?:BurstCompile|IJob\w*)\b', mask):
        skip('Burst/job file excluded')
        return text, done, skipped
    if re.search(r'^\s*#', mask, re.M):
        skip('preprocessor directives require manual review')
        return text, done, skipped
    # Reject malformed brace structure rather than guessing closing positions.
    stack, closes = [], {}
    for pos, char in enumerate(mask):
        if char == '{':
            stack.append(pos)
        elif char == '}':
            if not stack:
                skip('unbalanced braces')
                return text, done, skipped
            closes[stack.pop()] = pos
    if stack:
        skip('unbalanced braces')
        return text, done, skipped
    # Only declarations beginning a line, no attributes on the same line,
    # no parameters/generics/explicit-interface members. Other candidates report.
    candidates = re.compile(r'^([ \t]*)([^\r\n;{}]*\b(' + TARGETS + r')\s*\([^\r\n{};]*\)[^\r\n{;]*)(\{|;)?', re.M)
    signature = re.compile(r'(?:(?:public|private|protected|internal)\s+)+(?:(?:override|virtual|sealed|new|static)\s+)*void\s+(?:Update|OnUpdate|LateUpdate|OnGUI)\s*\(\s*\)\s*$')
    # Unmanaged ISystem update (Burst files are already excluded above).
    isystem = re.compile(r'(?:public\s+)?void\s+OnUpdate\s*\(\s*ref\s+SystemState\s+\w+\s*\)\s*$')
    # Harmony prefix/postfix by naming convention; parameters stay untouched.
    harmony = re.compile(r'(?:(?:public|private|internal)\s+)*static\s+(?:void|bool)\s+(?:Prefix|Postfix)\s*\([^()]*\)\s*$')
    is_patch_file = re.search(r'\bHarmonyPatch\b', mask) is not None
    edits = []
    for match in candidates.finditer(mask):
        decl = match.group(2).strip()
        method = match.group(3)
        line = text.count('\n', 0, match.start()) + 1
        if method in ('Prefix', 'Postfix'):
            if not is_patch_file:
                skip('Prefix/Postfix outside a HarmonyPatch file', line, method)
                continue
            if not harmony.fullmatch(decl):
                skip('unsupported/ambiguous signature', line, method)
                continue
        elif not (signature.fullmatch(decl) or isystem.fullmatch(decl)):
            skip('unsupported/ambiguous signature', line, method)
            continue
        pos = match.end(2)
        while pos < len(mask) and mask[pos].isspace():
            pos += 1
        if pos >= len(mask) or mask[pos] != '{':
            skip('not block-bodied', line, method)
            continue
        end = closes[pos]
        body = mask[pos + 1:end]
        if re.search(r'\b(?:yield|await|async|unsafe|fixed|stackalloc)\b', body):
            skip('iterator/async/unsafe body requires manual instrumentation', line, method)
            continue
        if not body.strip():
            skip('empty body', line, method)
            continue
        if any(a < pos < b for a, b, _ in edits):
            skip('nested candidate', line, method)
            continue
        label = f'{relative}:{line}.{method}'
        literal = json.dumps(label, ensure_ascii=True)
        wrapped = '\nusing (global::ModDiagnostics.Recorder.Measure(' + literal + '))\n{' + text[pos + 1:end] + '\n}\n'
        edits.append((pos + 1, end, wrapped))
        done.append(dict(file=relative, line=line, method=method, label=label))
    for start, end, replacement in reversed(edits):
        text = text[:start] + replacement + text[end:]
    if not done and not skipped:
        skip('no supported lifecycle declarations')
    return text, done, skipped


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read_mod(root):
    manifest_bytes = (root / 'ModManifest.json').read_bytes()
    manifest = json.loads(manifest_bytes.decode('utf-8-sig'))
    name = manifest['name']
    if not re.fullmatch(r'[A-Za-z0-9_-]+', name):
        raise ValueError('unsafe manifest name')
    files = {'ModManifest.json': manifest_bytes}
    seen = {'modmanifest.json'}
    for entry in manifest['files']:
        relative = entry['path'].replace('\\', '/')
        p = Path(relative)
        if p.is_absolute() or ':' in relative or '..' in p.parts or relative.casefold() in seen:
            raise ValueError('unsafe or duplicate manifest path: ' + relative)
        actual = (root / p).resolve()
        if not actual.is_relative_to(root) or not actual.is_file():
            raise ValueError('missing or escaping manifest file: ' + relative)
        seen.add(relative.casefold())
        files[relative] = actual.read_bytes()
    return manifest, files


def stage(source, output, no_instrument=False, companion=None):
    source, output = Path(source).resolve(), Path(output).resolve()
    companion = Path(companion or Path(__file__).resolve().parents[2] / 'ModDiagnostics').resolve()
    if output.exists() or output.is_relative_to(source) or output.is_relative_to(companion):
        raise ValueError('output must be a new directory outside source and recorder')
    manifest, files = read_mod(source)
    recorder_manifest, recorder_files = read_mod(companion)
    if manifest['name'].casefold() == recorder_manifest['name'].casefold() or recorder_manifest['name'] != 'ModDiagnostics':
        raise ValueError('source must differ from ModDiagnostics companion')
    mode = 'recorder-only' if no_instrument else 'instrumented'
    all_input = {manifest['name'] + '/' + k: v for k, v in files.items()}
    all_input.update({'ModDiagnostics/' + k: v for k, v in recorder_files.items()})
    fingerprints = {k: digest(v) for k, v in sorted(all_input.items())}
    source_hash = digest(json.dumps(fingerprints, sort_keys=True).encode())
    metadata = dict(schema=1, mode=mode, sourceHash=source_hash,
                    buildIdentity=source_hash + ':' + mode, inputHashes=fingerprints,
                    instrumented=[], skipped=[])
    staged = {}
    for relative, data in files.items():
        if relative.lower().endswith('.cs'):
            if no_instrument:
                metadata['skipped'].append(dict(file=relative, reason='recorder-only mode'))
            else:
                updated, done, skipped = instrument(data.decode('utf-8-sig'), relative)
                metadata['instrumented'].extend(done)
                metadata['skipped'].extend(skipped)
                if done:
                    data = updated.encode('utf-8')
        staged[manifest['name'] + '/' + relative] = data
    if metadata['instrumented']:
        dependencies = manifest.setdefault('dependencies', [])
        existing = next((d for d in dependencies if d.get('modName') == 'ModDiagnostics'), None)
        if existing is None:
            dependencies.append(dict(modName='ModDiagnostics', required=True))
        else:
            existing['required'] = True
        staged[manifest['name'] + '/ModManifest.json'] = (json.dumps(manifest, indent=2) + '\n').encode()
    replacements = 0
    for relative, data in recorder_files.items():
        if relative.lower().endswith('.cs'):
            text = data.decode('utf-8-sig')
            text, count = re.subn(r'public const string BuildIdentity = "[^"\r\n]*";',
                                 'public const string BuildIdentity = "' + metadata['buildIdentity'] + '";', text)
            replacements += count
            if count:
                data = text.encode('utf-8')
        staged['ModDiagnostics/' + relative] = data
    if replacements != 1:
        raise ValueError('expected exactly one recorder BuildIdentity declaration')
    metadata['outputHashes'] = {k: digest(v) for k, v in sorted(staged.items())}
    # Validate and construct everything before creating any output.
    output.mkdir(parents=True, exist_ok=False)
    for relative, data in staged.items():
        destination = output / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(data)
    (output / 'stage-metadata.json').write_text(json.dumps(metadata, indent=2) + '\n', encoding='utf-8')
    return metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--no-instrument', action='store_true')
    args = parser.parse_args()
    try:
        report = stage(args.source, args.output, args.no_instrument)
    except (ValueError, OSError, KeyError, UnicodeError) as exc:
        parser.exit(2, 'Cannot stage diagnostics: ' + str(exc) + '\n')
    print(json.dumps(dict(output=str(args.output.resolve()), buildIdentity=report['buildIdentity'],
                          instrumented=len(report['instrumented']), skipped=len(report['skipped'])), indent=2))


if __name__ == '__main__':
    main()
