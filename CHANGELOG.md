# Changelog

## 1.1.0

### New Features

- **New Trigger Types:** Added Gold, Level, Combo, and Health triggers.
  - `gold:N` — Grants a reward after collecting N gold.
  - `level:N` — Grants a reward every N levels gained.
  - `combo:N` — Grants a reward upon reaching a kill combo of N; use `ctime=N` to set the time window.
  - `health:N` — Grants a reward when HP drops below N%.
- **Random Trigger:** Added a new `random` trigger that picks a sub-trigger type at random each cycle. Customizable with `rtime`, `rkills`, `rstage`, and `rboss` options.
- **MaxGrants Support:** Added `max=N` option to cap how many times a rule can fire per run.
- **Cooldown Repeat Mode:** Added `mode=cooldown|cd=N` to enforce an N-second cooldown between grants.
- **OneShot Repeat Mode:** Added `mode=oneshot` so a rule fires exactly once and then disables itself.
- **Item Pool & Weighted Probability:** Multiple items can be defined with comma separation; use `%N` syntax to assign per-item weights (e.g. `Ring%60,None%40`).
- **`None` Item:** Using `None` as an item shows an "Unlucky" notification without granting anything, enabling chance-based rules.
- **Stage Detection via Harmony Patch:** Added a Harmony postfix on `MapController.StartNewMap` for reliable stage-transition detection.

### Bug Fixes

- Deleting or editing a rule no longer resets progress on other rules; `ReloadRules` now matches existing state by `trigger:interval` key.
- Kill, BossKill, Gold, Level, and Combo rules now preserve progress across stage changes; only Time and Random(Time) rules reset on a new stage.
- Kill/gold/level delta baselines are updated before `HandleStageStarted` fires, preventing a false delta spike on stage transition.
- `new Random()` was being re-instantiated on every "Unlucky" notification; `ItemGrantService` and `RewardRule` now use static `Random` instances.
- The invalid-item cache is cleared on rule reload (`ClearInvalidCache`) so corrected item names are retried on the next grant.
- `ComboArmed` and `HealthArmed` flags are now correctly transferred during `ReloadRules`.
- `ComboTimestamps` queue is preserved across `ReloadRules` and stage transitions.
- `RandomTargetTrigger` state is saved during `ReloadRules`, preventing unnecessary re-selection.

### Improvements

- **Notification System Rewrite:** `NotificationService` was rewritten from scratch with slide-in animation, color coding (green = reward, red = unlucky, yellow = warning), and a stack limit of 2 active notifications.
- **Multi-Layer Icon Lookup:** `ItemIconService` now tries `ItemData.eItem` matching, then sprite name similarity, then texture name similarity; generic Unity APIs are called via reflection for IL2CPP compatibility.
- **Embedded `nothing.raw` Icon:** A 64×64 embedded raw texture is used for the `None` item, bypassing IL2CPP native memory restrictions.
- **Two-Stage Item Grant:** When granting an item, `Inventory.AddItem` is attempted first; `ItemManager.GrantItem` is used as a fallback.
- **`WarnOnce` Mechanism:** Duplicate item-grant warning messages are suppressed so the same error is only logged once.

## 1.0.1

- Fixed trigger dropdown selection and other dropdown hit-testing issues in the editor.
- Fixed the editor overlay staying active after saving a rule.
- Fixed modal dropdown behavior so outside clicks now close the open selector.
- Improved the in-game UI structure and styling with a cleaner, more minimal layout.
- Improved item discovery and preview behavior.
- Added support for multiple items per rule.
- Live Preview now shows item icons instead of text labels like "Reward: 1x Ring".
- Reward items can now be configured as multiple items in a single rule.

## 1.0.0

- Initial public release.
