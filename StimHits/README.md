# Stim Hits

Replaces the take-damage sound with a crisp metallic ding when you hit something and when you get
hit. Every option is on the **Mod Options → Stim Hits** page; set a sound to **Off** to get the
vanilla sound back.

| Option | Default | |
|---|---|---|
| Hit sound | Stim | Played when you damage a creature (melee, bow, gun, staff). Off = vanilla. |
| Hit volume / pitch | 0.8 / 1.0 | Pitch above 1 = higher, snappier ding. |
| Hit range (tiles) | 16 | Vanilla take-damage sounds of creatures this close are muted (and objects ding within it). |
| Ding on objects too | Off | Also ding on ore boulders, destructibles etc. |
| Hurt sound | Stim | Played when you take damage. Off = vanilla. |
| Hurt volume / pitch | 0.9 / 1.0 | |

Sounds: **Stim** (the mod's own synthesized ting/clang), **Clang**, **Small Clang**, **Ding**,
**Anvil**, **Bell**, **Shield** (game sounds), **Custom** (your own file).

## Custom sounds

Put `hit.mp3` and/or `hurt.mp3` (`.ogg` and `.wav` work too) in

    %USERPROFILE%\AppData\LocalLow\Pugstorm\Core Keeper\StimHits\

and pick **Custom**. The folder lives outside the mod, so updates never delete your sounds. Files are
read at startup and again whenever you switch a sound to Custom. If a file is missing, Stim plays
instead. Keep them short (under half a second sounds best).

## Limits

- Hits are attributed through the damage numbers: your melee and your own projectiles ding; pets,
  minions and other players do not. Their creature-hurt sounds are still muted within the range.
- Replaced: the creature's hurt sound, the melee impact sound and the impact sound of your
  projectiles that hit. Flash, particles and enemy death sounds stay vanilla.
- Client-side only: other players hear their own settings.

Requires CoreLib and Mod Options. Built into Sid's Overhaul.
