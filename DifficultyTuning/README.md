# Difficulty Tuning

![Difficulty Tuning: original pixel-art illustration](../release/difficultytuning_logo.png)

Adjust enemy damage, health and speed to find a challenge that suits your world.

Previously called **Hard Mode Tuning**. There are separate profiles for hard and normal worlds, so you can tune either without changing the other.

## Start here

Open **Settings > Mod Options**, then choose **Difficulty Tuning (Hard)** or **Difficulty Tuning (Normal)**. Change the settings and **reload the world** to apply them.

With the defaults, regular enemies in hard worlds deal **1.5x normal damage** instead of the usual 2x. Their health stays at the usual hard-mode 1.5x. Bosses keep their normal hard-mode strength. Normal worlds are unchanged by default.

## What you can adjust

| Setting | Hard-world default | Normal-world default |
|---|---|---|
| Regular enemy damage | 1.5x normal | 1x normal |
| Regular enemy health | 1.5x normal | 1x normal |
| Enemy movement, projectile and attack recharge speed | 1x | 1x |
| Boss damage and health | 1x vanilla hard mode | 1x vanilla normal mode |

Regular damage and health range from 1x–2x in hard worlds and 0.5x–2x in normal worlds. Speed choices range from 0.9x–3x. Boss damage and health range from 0.9x–1.5x of the chosen world's usual values.

**Movement boosts taper for already-fast enemies.** At a 2x setting, slower enemies get the full 2x speed; the fastest get 1.1x. Settings below 1x reduce speed directly. Projectile speed and attack recharge have their own controls. A 1.25x recharge setting means attacks recover faster, with 80% of the usual cooldown.

Boss speed and attack patterns are not adjusted. Loot is unchanged.

## Requirements

Requires **CoreLib** and **Mod Options** on the host/server and every client. The host's settings determine enemy stats. Settings take effect the next time a world loads.

Also included in **Sid's Overhaul**; use one version of this feature.
