using System;
using System.Linq;
using System.Text;
using CoreLib.Data.Configuration;
using ModOptions;
using PugMod;

class DiscoveryTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static ConfigEntry<T> Entry<T>(ConfigFile file, string key, T value, AcceptableValueBase range = null,
        ConfigScope scope = null, string description = "Player-facing setting", params object[] tags) =>
        file.Bind("General", key, value, new ConfigDescription(description, range, tags), scope ?? new ConfigScope(ConfigAccessLevel.Client));
    static void Main()
    {
        const string path = "PlacementPlus/PlacementPlus.cfg";
        var fs = API.ConfigFilesystem;
        fs.Files[path] = Encoding.UTF8.GetBytes("[General]\nMaxBrushSize = 12\nUnknownFutureKey = keep me\n");
        var file = new ConfigFile(path, false);
        var size = Entry(file, "MaxBrushSize", 9, new AcceptableValueRange<int>(3, 15));
        var toggle = Entry(file, "SledgehammerPreview", true);
        var opacity = Entry(file, "SledgehammerPreviewOpacity", .15f, new AcceptableValueRange<float>(.02f, 1f));
        var restart = Entry(file, "RestartOption", false, scope: new ConfigScope(ConfigAccessLevel.Client, true));
        Entry(file, "Unbounded", 7);
        Entry(file, "Hidden", true, tags: new object[] { "Browsable=false" });
        Entry(file, "DebugLogging", false);
        Entry(file, "Undocumented", true, description: "");
        Entry(file, "ServerOnly", false, scope: new ConfigScope(ConfigAccessLevel.Server));
        Entry(file, "AdminOnly", false, scope: new ConfigScope(ConfigAccessLevel.Admin));
        Entry(file, "ReadOnly", false, scope: new ConfigScope(ConfigAccessLevel.ViewOnly));
        Entry(file, "FreeText", "abc");
        Entry(file, "UnknownScope", false, scope: ConfigScope.Empty);
        var choice = Entry(file, "Choice", "B", new AcceptableValueList<string>("A", "B", "C"));
        int writes = fs.Writes;
        var before = fs.Read(path).ToArray();
        var pages = ConfigDiscovery.Discover(new[] { file }, new[] { "PlacementPlus" }, (_, __) => false);
        Check(pages.Count == 1 && pages[0].Items.Count == 5, "supported types/scopes/filtering");
        Check(fs.Writes == writes && before.SequenceEqual(fs.Read(path)), "discovery must not write");
        Check(size.Value == 12 && file.OrphanedEntries.Values.Contains("keep me"), "saved values/orphans retained");
        var sized = pages[0].Items.Single(i => i.Label == "Max Brush Size");
        int events = 0; size.SettingChanged += (_, __) => events++;
        sized.Step(1);
        Check(size.Value == 13 && events == 1 && fs.Writes == writes + 1, "edit original entry, save and notify once");
        Check(Encoding.UTF8.GetString(fs.Read(path)).Contains("UnknownFutureKey = keep me"), "edit preserves orphan");
        sized.Step(1); sized.Step(1); sized.Step(1);
        Check(size.Value == 15, "range clamping");
        Check(ConfigDiscovery.Adapt(restart).Description.StartsWith("Restart required"), "restart metadata");
        Check(ConfigDiscovery.Adapt(toggle).Description == toggle.Description.Description, "no live boilerplate");
        ConfigDiscovery.Adapt(choice).Step(1);
        Check(choice.Value == "C", "choice cycles");
        var floating = ConfigDiscovery.Adapt(opacity); floating.Step(1);
        Check(Math.Abs(opacity.Value - .199f) < .00001f, "float range stepping");
        var large = Entry(file, "LargeRange", int.MaxValue, new AcceptableValueRange<int>(int.MinValue, int.MaxValue));
        ConfigDiscovery.Adapt(large).Step(1); Check(large.Value == int.MaxValue, "int overflow safe");
        Check(ConfigDiscovery.Discover(new[] { file, file }, new[] { "PlacementPlus" }, (_, __) => false).Count == 1, "repeated reference dedup");
        var duplicate = new ConfigFile("placementplus\\PlacementPlus.cfg", false);
        Check(ConfigDiscovery.Discover(new[] { file, duplicate }, new[] { "PlacementPlus" }, (_, __) => false).Count == 0, "ambiguous path rejected");
        Check(ConfigDiscovery.Discover(new[] { file }, new[] { "OtherMod" }, (_, __) => false).Count == 0, "unloaded mod hidden");
        Check(ConfigDiscovery.NormalizePath("PlacementPlus/../private.cfg") == "", "noncanonical path rejected");
        file.SaveOnConfigSet = false;
        Check(ConfigDiscovery.Discover(new[] { file }, new[] { "PlacementPlus" }, (_, __) => false).Count == 0, "deferred-save config hidden");
        int old = size.Value; sized.Step(-1); Check(size.Value == old, "stale adapter rechecks write policy");
        file.SaveOnConfigSet = true;
        API.ModLoader.LoadedMods.Add(new LoadedMod { Metadata = new Metadata { name = "PlacementPlus" } });
        Check(ConfigDiscovery.Scan().Count == 0, "registry also suppresses duplicate paths");
        var other = new ConfigFile("LateMod/config.cfg", false);
        Entry(other, "Toggle", true);
        API.ModLoader.LoadedMods.Add(new LoadedMod { Metadata = new Metadata { name = "LateMod" } });
        Check(ConfigDiscovery.Scan().Count == 1, "late configs discovered on next scan");
        new SettingsPage("Authored", "", other, "LateMod").Build();
        Check(ConfigDiscovery.Scan().Count == 0, "late authored page suppresses discovery");
        Check(SettingsPages.All.Count == 1 && SettingsPages.All[0].Title == "Authored", "discovery does not alter authored pages");
        Check(fs.Read(path).Length > 0 && file.OrphanedEntries.Values.Contains("keep me"), "config still intact");
        var installed = InstalledFixture.Create();
        writes = fs.Writes;
        var installedPages = ConfigDiscovery.Discover(new[] { installed }, new[] { "PlacementPlus" }, (_, __) => false);
        Check(installedPages.Count == 1 && installedPages[0].Items.Count == 6, "all six installed Placement Plus declarations discovered");
        Check(fs.Writes == writes, "installed settings discovery read-only");
        Check(installedPages[0].Title == "Placement Plus" && installedPages[0].Description == "", "friendly title without file path or boilerplate");
        Check(installedPages[0].Items.Any(i => i.Label == "Sledgehammer Preview Opacity"), "readable setting labels");
        Check(ConfigDiscovery.FriendlyName("UI_scaleFactor") == "UI scale Factor", "acronyms preserved");
        var named = ConfigDiscovery.Discover(new[] { installed }, new[] { "PlacementPlus" }, (_, __) => false, _ => "Placement Plus!");
        Check(named[0].Title == "Placement Plus!", "manifest display name preferred");
        var secondFile = new ConfigFile("PlacementPlus/VisualOptions.cfg", false);
        Entry(secondFile, "ShowPreview", true);
        var multiple = ConfigDiscovery.Discover(new[] { installed, secondFile }, new[] { "PlacementPlus" }, (_, __) => false);
        Check(multiple.Count == 2 && multiple.Select(p => p.Title).Distinct().Count() == 2 &&
            multiple.Any(p => p.Title == "Placement Plus - Visual Options") && multiple.All(p => !p.Title.Contains(".cfg")),
            "multiple configs get distinct readable titles");
        Console.WriteLine("PASS: " + checks + " discovery/configuration regression checks (real CoreLib config implementation).");
    }
}
