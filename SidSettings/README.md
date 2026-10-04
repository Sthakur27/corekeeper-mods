# Sid's Settings

The settings menu used by Sid's Core Keeper mods. Adds **Sid's Mods** to the game's Settings menu
(main menu and pause menu): a list of mods, and one page per mod with its options.

- Left/right (keyboard, controller) or click the `<` / `>` side of a value to change it.
- Each page shows the mod's description and has **Reset to defaults** (click twice to confirm).
- Values are saved immediately to `<mod name>/config.cfg` in the game's mod config folder (CoreLib).

Independent of Mod Settings Menu: both can be installed at the same time, each with its own button.
Built into Sid's Overhaul; the standalone mods depend on this one.

## For modders

```csharp
using SidSettings;

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

Options: `Toggle` (bool), `Choice` (string list, wraps around), `Stepper` (int range), `Slider`
(float range with a step). `SettingsPages.Create(mod, title, keyPrefix)` lets one mod own several
pages in one config file (Sid's Overhaul uses it to keep the keys of earlier versions).

How the menu works: the game's Gameplay settings screen is cloned twice at runtime (list and page);
their rows are replaced with `SettingsRow` components (subclass of `RadicalMenuOption`), and the
pages are pushed with `MenuManager.PushMenu`, so scrolling, navigation and Back are vanilla. The
Settings menu gets an extra row cloned from "Gameplay settings". Requires CoreLib.
