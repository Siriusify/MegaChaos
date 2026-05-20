using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaChaos.Services;

internal static class RuleScheduler
{
    private static readonly List<RuleState> Rules = new();
    private static readonly System.Random RandomPicker = new();
    private static int _lastKills;
    private static int _lastBossKills;
    private static int _lastGold;
    private static int _lastLevel;
    private static float? _lastGameTime;
    private static float _chaosTimer;

    public static IReadOnlyList<RuleState> GetRuleStates() => Rules;

    public static void ReloadRules()
    {
        // Clear the invalid-item cache so corrected/changed item names are retried.
        ItemGrantService.ClearInvalidCache();

        // Snapshot progress keyed by "trigger:interval" only.
        // This means changing an item name or toggling enabled does NOT reset progress;
        // only changing the trigger type or interval starts fresh.
        var snapshot = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Queue<RuleState>>(Rules.Count);
        foreach (var state in Rules)
        {
            var key = $"{state.Rule.Trigger}:{state.Rule.Interval}";
            if (!snapshot.TryGetValue(key, out var q))
                snapshot[key] = q = new System.Collections.Generic.Queue<RuleState>();
            q.Enqueue(state);
        }

        Rules.Clear();
        var raw = ConfigService.CurrentRules;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (var ruleText in raw.Split(';'))
            {
                if (!RewardRule.TryParse(ruleText, out var rule, out var error))
                {
                    if (!string.IsNullOrWhiteSpace(ruleText))
                        Main.Error($"Failed to parse rule '{ruleText}': {error}");
                    continue;
                }

                var newState = new RuleState(rule);
                var key = $"{rule.Trigger}:{rule.Interval}";
                if (snapshot.TryGetValue(key, out var q) && q.Count > 0)
                {
                    var saved = q.Dequeue();
                    // Restore in-game progress — item/enabled changes preserve state
                    newState.Progress = saved.Progress;
                    newState.GrantsCount = saved.GrantsCount;
                    newState.ArmedAtSeconds = saved.ArmedAtSeconds;
                    newState.LastTriggeredGameTime = saved.LastTriggeredGameTime;
                    newState.DebugStatus = saved.DebugStatus;
                    newState.RandomTargetTrigger = saved.RandomTargetTrigger;
                    newState.ComboArmed = saved.ComboArmed;
                    newState.HealthArmed = saved.HealthArmed;
                    foreach (var ts in saved.ComboTimestamps)
                        newState.ComboTimestamps.Enqueue(ts);
                }
                else
                {
                    var t = GameTimeService.TryGetElapsedStageSeconds();
                    newState.ArmedAtSeconds = t ?? 0f;
                }

                Rules.Add(newState);
            }
        }

        var gameTime = GameTimeService.TryGetElapsedStageSeconds();
        _lastGameTime = gameTime;
    }

    public static void ResetRun(float now)
    {
        _lastKills = RunStatService.GetKills();
        _lastBossKills = RunStatService.GetBossKills();
        _lastGold = RunStatService.GetGold();
        _lastLevel = RunStatService.GetLevel();
        var gameTime = GameTimeService.TryGetElapsedStageSeconds();
        _lastGameTime = gameTime;
        foreach (var rule in Rules)
        {
            rule.Reset(gameTime ?? 0f);
        }
    }

    public static void HandleStageStarted(float now)
    {
        // Update kill/gold/level baselines so next tick deltas are accurate
        _lastKills = RunStatService.GetKills();
        _lastBossKills = RunStatService.GetBossKills();
        _lastGold = RunStatService.GetGold();
        _lastLevel = RunStatService.GetLevel();
        var gameTime = GameTimeService.TryGetElapsedStageSeconds();
        _lastGameTime = gameTime;

        foreach (var state in Rules)
        {
            // Time-based rules must reset: the stage timer resets to 0
            if (state.Rule.Trigger == RewardTrigger.Time)
            {
                state.Progress = 0f;
                state.ArmedAtSeconds = gameTime ?? 0f;
            }
            // Random trigger: only reset if its current sub-target is Time
            else if (state.Rule.Trigger == RewardTrigger.Random &&
                     state.RandomTargetTrigger == RewardTrigger.Time)
            {
                state.Progress = 0f;
                state.ArmedAtSeconds = gameTime ?? 0f;
            }
            // Kill / BossKill / Gold / Level / Combo / Health:
            //   progress intentionally preserved across stage changes

            if (!state.Rule.Enabled)
                continue;

            if (state.Rule.Trigger == RewardTrigger.NewStage)
            {
                state.DebugStatus = "Triggered: New stage";
                TryGrant(state, gameTimeOverride: gameTime ?? now);
            }
            else if (state.Rule.Trigger == RewardTrigger.Random &&
                     state.RandomTargetTrigger == RewardTrigger.NewStage)
            {
                state.DebugStatus = "Random: new stage";
                TryGrant(state, gameTimeOverride: gameTime ?? now);
            }
        }
    }

    public static void Tick(float now)
    {
        int currentKills = RunStatService.GetKills();
        int currentBossKills = RunStatService.GetBossKills();
        int currentGold = RunStatService.GetGold();
        int currentLevel = RunStatService.GetLevel();
        float? currentHealth = RunStatService.GetHealthPercentage();
        float? gameTime = GameTimeService.TryGetElapsedStageSeconds();

        // Stage-change detection: update baselines BEFORE the early return
        // so the next tick doesn't see a false delta spike.
        if (_lastGameTime.HasValue && gameTime.HasValue && gameTime.Value < _lastGameTime.Value - 10f)
        {
            Main.Msg($"Detected new stage! Timer dropped from {_lastGameTime.Value:F1} to {gameTime.Value:F1}");
            _lastKills = currentKills;
            _lastBossKills = currentBossKills;
            _lastGold = currentGold;
            _lastLevel = currentLevel;
            _lastGameTime = gameTime;
            HandleStageStarted(now);
            return;
        }

        int deltaKills = currentKills - _lastKills;
        int deltaBossKills = currentBossKills - _lastBossKills;
        int deltaGold = currentGold - _lastGold;
        int deltaLevel = currentLevel - _lastLevel;

        var profile = ProfileManager.ActiveProfile;
        if (profile != null && profile.ChaosEnabled)
        {
            _chaosTimer += Time.unscaledDeltaTime; // Unscaled so it's consistent with game logic, but pauses when game is paused.
            if (_chaosTimer >= profile.ChaosInterval)
            {
                _chaosTimer = 0f;
                Chaos.ChaosEngine.Instance.TriggerRandomEffect();
            }
        }

        // Log gold and health for debugging
        if (deltaGold != 0 || currentGold > 0)
            Main.Msg($"Gold: {_lastGold} -> {currentGold} (delta: {deltaGold}), Health: {currentHealth}");

        _lastKills = currentKills;
        _lastBossKills = currentBossKills;
        _lastGold = currentGold;
        _lastLevel = currentLevel;
        _lastGameTime = gameTime;

        foreach (var state in Rules)
        {
            if (!state.Rule.Enabled) continue;

            if (state.Rule.MaxGrants > 0 && state.GrantsCount >= state.Rule.MaxGrants)
            {
                state.DebugStatus = $"Maxed ({state.GrantsCount}/{state.Rule.MaxGrants})";
                continue;
            }

            // 1. Time Trigger
            if (state.Rule.Trigger == RewardTrigger.Time)
            {
                if (!gameTime.HasValue)
                {
                    state.DebugStatus = "Waiting for game timer";
                    continue;
                }

                state.Progress = Math.Max(0f, gameTime.Value - state.ArmedAtSeconds);
                state.DebugStatus = $"Waiting {state.Progress:0.0}/{state.Rule.Interval}s";
                if (state.Progress >= state.Rule.Interval)
                {
                    TryGrant(state, gameTimeOverride: gameTime.Value);
                }
            }
            // 2. Kills Trigger
            else if (state.Rule.Trigger == RewardTrigger.Kills)
            {
                if (deltaKills > 0) state.Progress += deltaKills;
                while (state.Progress >= state.Rule.Interval && state.Rule.Interval > 0)
                {
                    if (!TryGrant(state, gameTimeOverride: gameTime ?? now)) break;
                }
                state.DebugStatus = $"Waiting {(int)state.Progress}/{state.Rule.Interval} kills";
            }
            // 3. BossKill Trigger
            else if (state.Rule.Trigger == RewardTrigger.BossKill)
            {
                if (deltaBossKills > 0) state.Progress += deltaBossKills;
                while (state.Progress >= state.Rule.Interval && state.Rule.Interval > 0)
                {
                    if (!TryGrant(state, gameTimeOverride: gameTime ?? now)) break;
                }
                state.DebugStatus = $"Waiting {(int)state.Progress}/{state.Rule.Interval} boss";
            }
            // 4. Gold Trigger
            else if (state.Rule.Trigger == RewardTrigger.Gold)
            {
                if (deltaGold > 0) state.Progress += deltaGold;
                while (state.Progress >= state.Rule.Interval && state.Rule.Interval > 0)
                {
                    if (!TryGrant(state, gameTimeOverride: gameTime ?? now)) break;
                }
                state.DebugStatus = $"Waiting {(int)state.Progress}/{state.Rule.Interval} gold";
            }
            // 5. Level Trigger
            else if (state.Rule.Trigger == RewardTrigger.Level)
            {
                if (deltaLevel > 0) state.Progress += deltaLevel;
                while (state.Progress >= state.Rule.Interval && state.Rule.Interval > 0)
                {
                    if (!TryGrant(state, gameTimeOverride: gameTime ?? now)) break;
                }
                state.DebugStatus = $"Waiting {(int)state.Progress}/{state.Rule.Interval} levels";
            }
            // 6. Combo Trigger
            else if (state.Rule.Trigger == RewardTrigger.Combo)
            {
                var comboValue = RunStatService.GetCurrentCombo();
                if (comboValue.HasValue)
                {
                    state.Progress = comboValue.Value;
                    state.DebugStatus = $"Combo {(int)state.Progress}/{state.Rule.Interval}";
                    if (comboValue.Value >= state.Rule.Interval)
                    {
                        if (state.ComboArmed)
                        {
                            state.ComboArmed = false;
                            TryGrant(state, gameTimeOverride: gameTime ?? now);
                        }
                    }
                    else
                    {
                        state.ComboArmed = true;
                    }
                }
                else
                {
                    var currentComboTime = gameTime ?? now;
                    var previousComboTime = _lastGameTime ?? currentComboTime;
                    var elapsed = Math.Max(0.001f, currentComboTime - previousComboTime);
                    if (deltaKills > 0)
                    {
                        var tickStart = currentComboTime - elapsed;
                        for (var i = 0; i < deltaKills; i++)
                        {
                            var portion = (i + 1f) / Math.Max(1f, deltaKills);
                            state.ComboTimestamps.Enqueue(tickStart + elapsed * portion);
                        }
                    }

                    while (state.ComboTimestamps.Count > 0 && (currentComboTime - state.ComboTimestamps.Peek()) > state.Rule.ComboTimeSeconds)
                    {
                        state.ComboTimestamps.Dequeue();
                    }

                    state.Progress = state.ComboTimestamps.Count;
                    state.DebugStatus = $"Combo {(int)state.Progress}/{state.Rule.Interval} in {state.Rule.ComboTimeSeconds}s";
                    if (state.Progress >= state.Rule.Interval)
                    {
                        TryGrant(state, gameTimeOverride: gameTime ?? now);
                    }
                }
            }
            // 7. Health Trigger
            else if (state.Rule.Trigger == RewardTrigger.Health && currentHealth.HasValue)
            {
                float threshold = state.Rule.Interval;
                bool isBelow = currentHealth.Value < threshold;
                state.DebugStatus = $"Health {currentHealth.Value:0}% / {threshold}%";

                if (isBelow)
                {
                    if (state.HealthArmed)
                    {
                        state.HealthArmed = false;
                        TryGrant(state, gameTimeOverride: gameTime ?? now);
                    }
                }
                else
                {
                    state.HealthArmed = true;
                }
            }
            else if (state.Rule.Trigger == RewardTrigger.NewStage)
            {
                state.DebugStatus = "Waiting for next stage";
            }
            else if (state.Rule.Trigger == RewardTrigger.Random)
            {
                if (state.RandomTargetTrigger == null)
                    SelectNextRandomTrigger(state);

                var target = state.RandomTargetTrigger ?? RewardTrigger.Time;
                state.DebugStatus = $"Random: {GetRandomTriggerLabel(target)}";

                if (target == RewardTrigger.Time)
                {
                    if (!gameTime.HasValue)
                    {
                        state.DebugStatus = "Random: waiting for game timer";
                        continue;
                    }

                    state.Progress = Math.Max(0f, gameTime.Value - state.ArmedAtSeconds);
                    state.DebugStatus = $"Random Time {state.Progress:0.0}/{state.Rule.RandomTimeSeconds}s";
                    if (state.Progress >= state.Rule.RandomTimeSeconds)
                        TryGrant(state, gameTimeOverride: gameTime.Value);
                }
                else if (target == RewardTrigger.Kills)
                {
                    if (deltaKills > 0)
                        state.Progress += deltaKills;

                    state.DebugStatus = $"Random Kills {(int)state.Progress}/{state.Rule.RandomKillCount}";
                    if (state.Progress >= state.Rule.RandomKillCount)
                        TryGrant(state, gameTimeOverride: gameTime ?? now);
                }
                else if (target == RewardTrigger.BossKill)
                {
                    if (deltaBossKills > 0)
                        state.Progress += deltaBossKills;

                    state.DebugStatus = $"Random Boss {(int)state.Progress}/1";
                    if (state.Progress >= 1f)
                        TryGrant(state, gameTimeOverride: gameTime ?? now);
                }
                else if (target == RewardTrigger.NewStage)
                {
                    state.DebugStatus = "Random: waiting for next stage";
                }
            }
        }

        _lastGameTime = gameTime;
    }

    private static bool TryGrant(RuleState state, float gameTimeOverride)
    {
        if (state.Rule.MaxGrants > 0 && state.GrantsCount >= state.Rule.MaxGrants)
        {
            state.DebugStatus = "Max grants reached";
            return false;
        }

        if (!CanGrant(state, gameTimeOverride, out var blockedReason))
        {
            state.DebugStatus = blockedReason;
            return false;
        }

        var itemToGrant = state.Rule.GetRandomItemFromPool();
        if (ItemGrantService.GrantItem(itemToGrant, state.Rule.Count))
        {
            state.GrantsCount++;
            state.LastTriggeredGameTime = gameTimeOverride;
            if (itemToGrant == "None")
                state.DebugStatus = "Missed (Luck)";
            else
                state.DebugStatus = $"Granted {state.Rule.Count}x {itemToGrant}";
            ResetAfterGrant(state, gameTimeOverride);
            return true;
        }
        else
        {
            state.DebugStatus = $"Grant failed: {itemToGrant}";
            return false;
        }
    }

    private static bool CanGrant(RuleState state, float now, out string blockedReason)
    {
        blockedReason = null;

        if (state.Rule.RepeatMode == RuleRepeatMode.OneShot && state.GrantsCount > 0)
        {
            blockedReason = "One-shot already granted";
            return false;
        }

        if (state.Rule.RepeatMode == RuleRepeatMode.Cooldown && state.LastTriggeredGameTime >= 0f)
        {
            var remaining = state.Rule.CooldownSeconds - (now - state.LastTriggeredGameTime);
            if (remaining > 0f)
            {
                blockedReason = $"Cooldown {remaining:0.0}s";
                return false;
            }
        }

        return true;
    }

    private static void ResetAfterGrant(RuleState state, float now)
    {
        switch (state.Rule.Trigger)
        {
            case RewardTrigger.Time:
                state.Progress = 0f;
                state.ArmedAtSeconds = now;
                break;
            case RewardTrigger.Kills:
            case RewardTrigger.BossKill:
            case RewardTrigger.Gold:
            case RewardTrigger.Level:
                state.Progress = Math.Max(0f, state.Progress - state.Rule.Interval);
                break;
            case RewardTrigger.Combo:
                state.ComboTimestamps.Clear();
                state.Progress = 0f;
                state.ComboArmed = false;
                break;
            case RewardTrigger.Random:
                state.Progress = 0f;
                state.ArmedAtSeconds = now;
                SelectNextRandomTrigger(state);
                break;
            case RewardTrigger.Health:
            case RewardTrigger.NewStage:
            default:
                state.Progress = 0f;
                state.ArmedAtSeconds = now;
                break;
        }
    }

    private static void SelectNextRandomTrigger(RuleState state)
    {
        var available = new List<RewardTrigger>(4);
        if (state.Rule.RandomAllowTime)
            available.Add(RewardTrigger.Time);
        if (state.Rule.RandomAllowKills)
            available.Add(RewardTrigger.Kills);
        if (state.Rule.RandomAllowNewStage)
            available.Add(RewardTrigger.NewStage);
        if (state.Rule.RandomAllowBossKill)
            available.Add(RewardTrigger.BossKill);

        if (available.Count == 0)
        {
            state.RandomTargetTrigger = null;
            return;
        }

        state.RandomTargetTrigger = available[RandomPicker.Next(available.Count)];
        state.Progress = 0f;
        state.ArmedAtSeconds = _lastGameTime ?? 0f;
    }

    private static string GetRandomTriggerLabel(RewardTrigger trigger)
    {
        return trigger switch
        {
            RewardTrigger.Time => "Time",
            RewardTrigger.Kills => "Kills",
            RewardTrigger.NewStage => "Stage",
            RewardTrigger.BossKill => "Boss",
            _ => trigger.ToString()
        };
    }

    internal sealed class RuleState
    {
        public RewardRule Rule { get; }
        public float Progress { get; set; }
        public int Milestone { get => GrantsCount; set => GrantsCount = value; }
        public int GrantsCount { get; set; }
        public float ArmedAtSeconds { get; set; }
        public Queue<float> ComboTimestamps { get; } = new();
        public bool ComboArmed { get; set; } = true;
        public bool HealthArmed { get; set; } = true;
        public float LastTriggeredGameTime { get; set; } = -1f;
        public string DebugStatus { get; set; } = "Idle";
        public RewardTrigger? RandomTargetTrigger { get; set; }

        public RuleState(RewardRule rule)
        {
            Rule = rule;
        }

        public void Reset(float now)
        {
            Progress = 0;
            GrantsCount = 0;
            ArmedAtSeconds = now;
            ComboTimestamps.Clear();
            ComboArmed = true;
            HealthArmed = true;
            LastTriggeredGameTime = -1f;
            DebugStatus = string.Empty;
            RandomTargetTrigger = null;
        }
    }
}
