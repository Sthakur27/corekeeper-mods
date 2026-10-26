import json
from pathlib import Path
import tempfile
import unittest

from stage_diagnostics import instrument, lexical_mask, stage


class InstrumentTests(unittest.TestCase):
    def test_strings_comments_and_returns(self):
        source = '''class A {
 public void Update()
 {
 // } void LateUpdate() { }
 var a = "}"; var b = @"a""{";
 var c = $"{{literal}} {Call("}")} end";
 if (a != null) return;
 throw new Exception("bad");
 }
}'''
        result, done, skipped = instrument(source, 'Scripts/A.cs')
        self.assertEqual(len(done), 1)
        self.assertFalse(skipped)
        self.assertIn('using (global::ModDiagnostics.Recorder.Measure(', result)
        self.assertIn('if (a != null) return;', result)
        self.assertIn('throw new Exception("bad");', result)
        masked = lexical_mask(result)
        self.assertEqual(masked.count('{'), masked.count('}'))
        self.assertIn('var c = $"{{literal}} {Call("}")} end";', result)

    def test_multiple_methods(self):
        source = 'class A {\n public void Update() { Foo(); }\n protected override void OnUpdate()\n { Bar(); }\n}'
        result, done, _ = instrument(source, 'A.cs')
        self.assertEqual(len(done), 2)
        self.assertEqual(result.count('Recorder.Measure'), 2)

    def test_skip_jobs_generated_and_directives(self):
        for source, path in [
            ('[BurstCompile] class A {\n public void Update() { Foo(); }\n}', 'A.cs'),
            ('class A : IJob {\n public void Update() { Foo(); }\n}', 'A.cs'),
            ('class A {\n public void Update() { Foo(); }\n}', 'Scripts/Generated/A.cs'),
            ('#if X\nclass A {\n public void Update() { Foo(); }\n}\n#endif', 'A.cs'),
        ]:
            with self.subTest(path=path, source=source):
                result, done, skipped = instrument(source, path)
                self.assertEqual(result, source)
                self.assertFalse(done)
                self.assertTrue(skipped)

    def test_skip_unsupported_and_empty(self):
        source = '''class A {
 public async void Update() { await Foo(); }
 private IEnumerator LateUpdate() { yield return null; }
 public void OnGUI() { /* empty */ }
 public void OnUpdate(int x) { Foo(); }
}'''
        result, done, skipped = instrument(source, 'A.cs')
        self.assertEqual(result, source)
        self.assertFalse(done)
        self.assertEqual(len(skipped), 4)

    def test_isystem_and_harmony_hooks(self):
        source = '''[HarmonyPatch(typeof(X), "Y")]
static class P {
 public static bool Prefix(ref int __result, X __instance) { if (__result > 0) return false; return true; }
 static void Postfix() { Bar(); }
}
partial struct S : ISystem {
 public void OnUpdate(ref SystemState state) { Baz(); }
}'''
        result, done, skipped = instrument(source, 'A.cs')
        self.assertEqual([d['method'] for d in done], ['Prefix', 'Postfix', 'OnUpdate'])
        self.assertFalse(skipped)
        self.assertIn('return false; return true;', result)
        masked = lexical_mask(result)
        self.assertEqual(masked.count('{'), masked.count('}'))

    def test_prefix_needs_harmony_file_and_static(self):
        source = 'class A {\n public static void Prefix() { Foo(); }\n public bool Postfix(int x) { return true; }\n}'
        result, done, skipped = instrument(source, 'A.cs')
        self.assertEqual(result, source)
        self.assertFalse(done)
        self.assertEqual(len(skipped), 2)
        patched = '[HarmonyPatch]\n' + source
        _, done, skipped = instrument(patched, 'A.cs')
        self.assertEqual([d['method'] for d in done], ['Prefix'])
        self.assertEqual(len(skipped), 1)

    def test_invalid_lexing(self):
        for source in ['class A {', 'class A { string x = "oops; }', 'var x = """raw""";']:
            result, done, skipped = instrument(source, 'A.cs')
            self.assertEqual(result, source)
            self.assertFalse(done)
            self.assertTrue(skipped)


class StageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / 'Input'
        self.recorder = self.root / 'Recorder'
        self.make_mod(self.source, 'Example', 'class A {\n public void Update() { return; }\n}')
        self.make_mod(self.recorder, 'ModDiagnostics', 'class R { public const string BuildIdentity = "recorder-only"; }')

    def tearDown(self):
        self.temp.cleanup()

    def make_mod(self, directory, name, code):
        directory.mkdir()
        (directory / 'Code.cs').write_text(code)
        (directory / 'private.txt').write_text('not runtime')
        (directory / 'ModManifest.json').write_text(json.dumps(dict(name=name, files=[dict(path='Code.cs', guid='')], dependencies=[])))

    def test_stage_identity_dependency_companion_and_source_unchanged(self):
        before = {p.name: p.read_bytes() for p in self.source.iterdir()}
        report = stage(self.source, self.root / 'Output', companion=self.recorder)
        target = self.root / 'Output'
        manifest = json.loads((target / 'Example/ModManifest.json').read_text())
        self.assertIn(dict(modName='ModDiagnostics', required=True), manifest['dependencies'])
        self.assertIn(report['buildIdentity'], (target / 'ModDiagnostics/Code.cs').read_text())
        self.assertFalse((target / 'Example/private.txt').exists())
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.source.iterdir()})
        again = stage(self.source, self.root / 'Output2', companion=self.recorder)
        self.assertEqual(report, again)

    def test_recorder_only(self):
        report = stage(self.source, self.root / 'Output', True, self.recorder)
        target = self.root / 'Output/Example'
        self.assertEqual((target / 'Code.cs').read_bytes(), (self.source / 'Code.cs').read_bytes())
        self.assertEqual(json.loads((target / 'ModManifest.json').read_text())['dependencies'], [])
        self.assertFalse(report['instrumented'])
        self.assertTrue(report['buildIdentity'].endswith(':recorder-only'))

    def test_output_safety(self):
        for target in [self.source, self.source / 'nested', self.recorder / 'nested', self.root]:
            with self.subTest(target=target), self.assertRaises(ValueError):
                stage(self.source, target, companion=self.recorder)
        self.assertFalse((self.source / 'nested').exists())

    def test_manifest_escape_fails_before_creating_output(self):
        manifest = json.loads((self.source / 'ModManifest.json').read_text())
        manifest['files'].append(dict(path='../secret.cs'))
        (self.source / 'ModManifest.json').write_text(json.dumps(manifest))
        with self.assertRaises(ValueError):
            stage(self.source, self.root / 'Output', companion=self.recorder)
        self.assertFalse((self.root / 'Output').exists())

    def test_all_skipped_has_no_dependency(self):
        (self.source / 'Code.cs').write_text('class A {\n public void Update() {}\n}')
        report = stage(self.source, self.root / 'Output', companion=self.recorder)
        self.assertFalse(report['instrumented'])
        self.assertEqual(json.loads((self.root / 'Output/Example/ModManifest.json').read_text())['dependencies'], [])


if __name__ == '__main__':
    unittest.main()
