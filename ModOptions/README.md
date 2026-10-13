# Mod Options

![Mod Options — illustrated mod cover](../release/modoptions_logo.png)

Change supported mod settings in one place, without leaving Core Keeper or editing configuration files. Use your mouse, keyboard or controller.

Mods with their own Mod Options pages appear alongside compatible settings discovered from other installed mods. Discovery is limited to supported client-side CoreLib settings; not every mod or option will appear.

## Get started

1. Subscribe to [CoreLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3673516180) and [Mod Options](https://steamcommunity.com/sharedfiles/filedetails/?id=3813531202), then restart Core Keeper.
2. Open **Settings > Mod Options** from the main menu or pause menu.
3. Choose a mod, then adjust its options. Settings save automatically.

Click the left or right side of a value, or use left/right navigation. On pages that offer **Reset to defaults**, select it twice to confirm. Automatically discovered settings do not have a bulk reset.

## Requirements and compatibility

Requires **CoreLib**. Keep it updated for automatic discovery support. For multiplayer, install this library where your chosen mods require it, including the host/server and clients when required. Automatically discovered options change local client settings; they do not synchronize server settings.

Built into **Sid's Overhaul**. The standalone mods use this separate library. It can coexist with Mod Settings Menu, which has its own button.

## Third-party settings

Compatible third-party mods appear directly in the main Mod Options list. Names and labels are made readable, and the original mod's help text appears beneath each setting. Reopen Mod Options to find settings initialized after the menu was opened. Opening the menu does not change saved values.

The original mod decides when a change takes effect. Options explicitly marked for restart show **Restart required**; some mods also cache settings, so restart if a change is not visible immediately. Your edits go back to that mod's existing configuration.

**Placement Plus** has been checked with six supported settings. Mods that use their own storage format, such as **Health Bars** and **Roofing Gadget Plus**, are not discovered. Server/admin controls, keybindings and free-text settings are also outside this feature's scope.

## For modders

### Automatic discovery scope

Discovery supports existing, described **client-scoped CoreLib** entries: booleans, bounded integers/floats, and bool/int/float/string acceptable-value lists. Integer controls move by one; float controls move by one twentieth of the declared range. Descriptions retain the original mod's help text. Only settings marked as needing a restart show **Restart required**; ordinary options have no status prefix. The original mod decides when changes take effect, so restart if an option is cached by that mod.

Edits use the original config entry, preserving its save behavior and change events. Discovery itself never creates config files, binds keys, reloads files, sanitizes saved values, or saves anything. There is no bulk reset for discovered settings. CoreLib preserves bound and orphaned values when saving; its normal save process may reformat comments and ordering.

The initial version deliberately excludes:

- Mods that already have an authored Mod Options page, including other configs owned by that mod.
- Server/admin/view-only settings; this feature does not implement multiplayer config synchronization.
- Free text, enums, keybinds, unbounded numbers, custom types, undocumented entries, and entries with custom metadata tags whose visibility/editability semantics are unknown.
- CoreLib's own configuration, common internal/debug/credential/security/ID-binding names, and files with automatic saving disabled. Name filtering is conservative, not a guarantee that every described option is appropriate for every player.
- Ambiguous duplicate live file instances, noncanonical paths, files outside `<loaded manifest name>/...`, and unsupported custom formats such as JSON.

Requires a CoreLib version with `ConfigFile.AllConfigFilesReadOnly`, `ConfigScope`, and acceptable-value metadata. No disk search or reflection into mod internals is used. Installed Placement Plus's six settings were validated; Health Bars and Roofing Gadget Plus use custom JSON and are outside this scope.

### Registering a settings page

```csharp
using ModOptions;

private static Setting<string> _speed;

public void Init()
{
    SettingsPages.Create(this, "My Mod")
        .Hint("What this mod does.")
        .Choice(out _speed, "Speed", new[] { "1x", "2x", "3x" }, "1x")
        .Build();
    _speed.OnChanged += token => Apply(token);
}
```

Controls: `Toggle` (bool), `Choice` (string list), `Stepper` (int range), and `Slider` (float range with a step). Read `Setting<T>.Value` or subscribe to `.OnChanged`.

`SettingsPages.Create(mod, title, keyPrefix)` supports several pages in one config file. Pages sort alphabetically; `.PinToTop()` places a page first. CoreLib saves values in `<mod name>/config.cfg`. The interface uses the game's menu navigation and scrolling.

### Validation

`python ModOptions/Tests/run.py --csc <path/to/csc.dll>` compiles and runs regression checks against the installed CoreLib configuration source and extracts the installed Placement Plus `Bind` declarations as an integration fixture. Override `--corelib` and `--placement` for different install locations. Tests use an in-memory filesystem and do not access player configs. Outputs go to ignored `build/modoptions-tests`.

Checks cover read-only discovery, original-entry events/saves, preservation of existing and orphaned values, filtering, ranges, metadata, duplicate paths, repeated references, late configs, and authored-page precedence. Standalone and assembled overhaul were also offline compile-checked against the game assemblies. Version 1.2.0 passed the game loader safety check and displayed the discovered settings in-game. Versions 1.2.1 and 1.2.2 also passed the loader safety check. Sid confirmed the final 1.2.2 layout in-game: discovered mods in the main list, clean names, and full descriptions underneath each setting. Persistence is covered by automated configuration tests; a manual save/restart value test was not separately confirmed.
