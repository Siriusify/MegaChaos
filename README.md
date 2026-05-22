# MegaChaos

**MegaChaos** is a massive 2-in-1 MelonLoader mod for **Megabonk**. Originally designed as a powerful tool to create fully customizable **Item Reward Rules** directly in-game, it now also features a brand new **Chaos Mode** that brings unpredictable madness to your runs!

Open the menu with **`F8`**.

---

## 🎁 Custom Reward Rules

The core foundation of the mod. Build your own custom item reward schedules without ever touching a config file!

- **Trigger Types:** Time, Kills, Combo, Health Drop, Gold, Level, New Stage, Boss, and Random.
- **Item Pools & Probability:** Assign multiple items to a single rule with individual probability weights (e.g., Key 60%, Medkit 30%, Unlucky 10%). If the roll hits an empty percentage, you get nothing!
- **Rule Modes:** Set rules to Repeat indefinitely, trigger as a One-Shot per run, or put them on a Cooldown timer.
- **Live Editing:** Progress is preserved across rule edits — changing an item or toggling a rule mid-game doesn't reset your progress.
- **Rule Management:** Enable or disable rules individually, and search, filter, or sort your rule list by trigger type or status.
- **Share:** Export and import your custom rule sets as JSON files.

---

## 🌪️ Chaos Mode (New in 1.2.0!)

Enable Chaos Mode to completely transform your game by dropping unpredictable, game-changing events at random intervals. 

### Over 25+ Unique Chaos Events
Survive a massive variety of chaotic effects, including:
- **Visual Distortions:** Complete blindness, drunk camera swaying, intense shaking, FOV zoom-ins, and horizontal screen flipping (Mirror World).
- **Game Mechanics:** Low/High gravity, disabling player attacks, hyper speed, and slow motion.
- **The Trolls:** "Packet Loss" (simulating a terrible internet connection with micro-freezes) and "HUD Less" (playing completely blind to your stats).
- **Extreme Danger:** "One Hit KO" mode where both you and all enemies die in a single hit, and "Mob Rain" which clones active enemies and drops them right on your head.
- **Lotteries & Audits:** High-risk lotteries that randomly grant or steal items, stats, and tomes. Watch out for the "Fake" variations that trick you into thinking you won, only to snatch the reward away 5 seconds later!

### Fully Customizable
You can adjust the frequency, duration multiplier, and intensity of the chaos directly from the in-game UI. You can easily keep track of active events and their remaining durations via the new on-screen GTA-style Chaos Overlay.

---

## Installation

1. Install [MelonLoader](https://melonwiki.xyz/#/?id=requirements) for Megabonk.
2. Place `MegaChaos.dll` inside your `Mods` folder.
3. Launch the game and press **`F8`** to open the MegaChaos menu.

---

## Notes

- All configurations (Reward rules and Chaos settings) are saved automatically to your MelonLoader preferences.
- Item names in rules must match the internal game names exactly (use the in-game item picker to be safe).
- Progress for kill/gold/level/boss/combo rules carries over between stages — only time-based rules reset on stage change.
