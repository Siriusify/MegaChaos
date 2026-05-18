# MegaChaos

**MegaChaos** is a MelonLoader mod for **Megabonk** that lets you create fully customizable item reward rules directly in-game — without ever touching a config file.

Open the menu with **`F8`**.

---

## Features

- Create, edit, and delete reward rules from an in-game menu
- Grant items based on a wide range of trigger types
- Multi-item pools with individual probability weights per item
- Live preview of your reward setup before saving
- Enable or disable rules individually without deleting them
- Search, filter, and sort your rule list by trigger type or status
- Export and import your rule sets as JSON files
- Progress is preserved across rule edits — changing an item or toggling a rule doesn't reset your in-game progress

---

## Available Triggers

| Trigger | Description |
|---------|-------------|
| **Time** | Every N seconds of stage time |
| **Kills** | Every N enemy kills |
| **Combo** | N kills within a time window |
| **Health Drop** | When HP falls below a threshold |
| **Gold** | Every N gold collected |
| **Level** | Every N player levels |
| **New Stage** | On every new stage start |
| **Boss** | On every boss kill |
| **Random** | Randomly picks from a pool of sub-triggers |

---

## Rule Options

Each rule supports the following settings:

| Option | Description |
|--------|-------------|
| **Trigger** | What event activates the rule |
| **Condition** | The threshold value (kills, seconds, gold, etc.) |
| **Item(s)** | One item or a weighted pool of multiple items |
| **Count** | How many of the item to grant |
| **Mode** | `Repeat`, `One Shot`, or `Cooldown` |
| **Max Grants** | Cap how many times the rule can fire (0 = unlimited) |

---

## Item Pools & Probability

You can assign multiple items to a single rule. Each item gets its own probability weight.

**Example:** `Key (60%), Medkit (30%), BeefyRing (10%)`

- Weights don't need to add up to 100. If the total is under 100, the remaining chance results in **no item** (displayed as "Unlucky").
- Use the item dropdown in the rule editor to add items and adjust weights with `+`/`-` buttons.

---

## Random Trigger

The **Random** trigger picks one sub-trigger each cycle from the pool you configure:

- **Time** — fires after N seconds
- **Kills** — fires after N kills
- **Stage** — fires on the next new stage
- **Boss** — fires on the next boss kill

After each grant, a new sub-trigger is randomly selected.

---

## Rule Modes

| Mode | Behavior |
|------|----------|
| **Repeat** | Fires every time the condition is met, indefinitely |
| **One Shot** | Fires once per run, then stops |
| **Cooldown** | After firing, waits N seconds before it can fire again |

---

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=requirements) for Megabonk.
2. Place `MegaChaos.dll` inside your `Mods` folder.
3. Launch the game and press **`F8`** to open the menu.

---

## Notes

- Rules are saved automatically to your MelonLoader preferences file.
- Item names must match the internal game names exactly (use the in-game item picker to be safe).
- Progress for kill/gold/level/boss/combo rules carries over between stages — only time-based rules reset on stage change.
- Debug logging can be enabled by setting the environment variable `MEGA_CHAOS_DEBUG=1`.
