using System;

namespace MegaChaos;

internal sealed class RewardRule
{
    private static readonly Random ItemPoolRandom = new();
    private static readonly object ItemPoolRandomLock = new();

    public RewardRule(
        bool enabled,
        RewardTrigger trigger,
        int interval,
        string itemName,
        int count,
        RuleRepeatMode repeatMode = RuleRepeatMode.Repeat,
        int cooldownSeconds = 0,
        int maxGrants = 0,
        int comboTimeSeconds = 5,
        int randomTimeSeconds = 30,
        int randomKillCount = 100,
        bool randomAllowTime = true,
        bool randomAllowKills = true,
        bool randomAllowNewStage = true,
        bool randomAllowBossKill = true)
    {
        Enabled = enabled;
        Trigger = trigger;
        Interval = interval;
        ItemName = itemName;
        Count = count;
        RepeatMode = repeatMode;
        CooldownSeconds = cooldownSeconds;
        MaxGrants = maxGrants;
        ComboTimeSeconds = comboTimeSeconds;
        RandomTimeSeconds = randomTimeSeconds;
        RandomKillCount = randomKillCount;
        RandomAllowTime = randomAllowTime;
        RandomAllowKills = randomAllowKills;
        RandomAllowNewStage = randomAllowNewStage;
        RandomAllowBossKill = randomAllowBossKill;
    }

    public bool Enabled { get; }

    public RewardTrigger Trigger { get; }

    public RuleRepeatMode RepeatMode { get; }

    public int CooldownSeconds { get; }

    public int MaxGrants { get; }

    public int ComboTimeSeconds { get; }

    public int Interval { get; }

    public string ItemName { get; }

    public string GetRandomItemFromPool()
    {
        if (string.IsNullOrWhiteSpace(ItemName))
            return string.Empty;

        if (!ItemName.Contains(","))
        {
            var match = System.Text.RegularExpressions.Regex.Match(ItemName, @"(.*?)(?:%(\d+)|\((\d+)%\)|(\d+)%)$");
            if (match.Success)
            {
                string name = match.Groups[1].Value.Trim();
                string wStr = match.Groups[2].Success ? match.Groups[2].Value :
                              match.Groups[3].Success ? match.Groups[3].Value :
                              match.Groups[4].Success ? match.Groups[4].Value : "";
                
                if (int.TryParse(wStr, out var weight))
                {
                    lock (ItemPoolRandomLock)
                    {
                        if (ItemPoolRandom.Next(0, 100) < weight)
                            return name;
                        return "None";
                    }
                }
                return name;
            }
            return ItemName;
        }

        var items = ItemName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (items.Length == 0)
            return string.Empty;

        var parsedItems = new System.Collections.Generic.List<System.Tuple<string, int>>();
        int totalWeight = 0;

        foreach (var item in items)
        {
            var name = item.Trim();
            int weight = 100;
            var match = System.Text.RegularExpressions.Regex.Match(name, @"(.*?)(?:%(\d+)|\((\d+)%\)|(\d+)%)$");
            if (match.Success)
            {
                name = match.Groups[1].Value.Trim();
                string wStr = match.Groups[2].Success ? match.Groups[2].Value :
                              match.Groups[3].Success ? match.Groups[3].Value :
                              match.Groups[4].Success ? match.Groups[4].Value : "";
                if (!string.IsNullOrEmpty(wStr) && int.TryParse(wStr, out var parsedWeight) && parsedWeight >= 0)
                {
                    weight = parsedWeight;
                }
            }
            else
            {
                var parts = name.Split('%');
                name = parts[0].Trim();
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsedWeight) && parsedWeight >= 0)
                    weight = parsedWeight;
            }

            parsedItems.Add(new System.Tuple<string, int>(name, weight));
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            var match = System.Text.RegularExpressions.Regex.Match(items[0], @"(.*?)(?:%(\d+)|\((\d+)%\)|(\d+)%)$");
            if (match.Success) return match.Groups[1].Value.Trim();
            return items[0].Split('%')[0].Trim();
        }

        int maxWeight = Math.Max(100, totalWeight);

        lock (ItemPoolRandomLock)
        {
            int roll = ItemPoolRandom.Next(0, maxWeight);
            if (roll >= totalWeight)
                return "None";

            int currentWeight = 0;
            foreach (var item in parsedItems)
            {
                currentWeight += item.Item2;
                if (roll < currentWeight)
                    return item.Item1;
            }
        }
        
        return parsedItems[parsedItems.Count - 1].Item1;
    }

    public int Count { get; }

    public int RandomTimeSeconds { get; }

    public int RandomKillCount { get; }

    public bool RandomAllowTime { get; }

    public bool RandomAllowKills { get; }

    public bool RandomAllowNewStage { get; }

    public bool RandomAllowBossKill { get; }

    public static bool TryParse(string rawRule, out RewardRule rule, out string error)
    {
        rule = null;
        error = null;

        if (string.IsNullOrWhiteSpace(rawRule))
        {
            error = "Empty rule";
            return false;
        }

        var optionParts = rawRule.Split('|');
        var parts = optionParts[0].Split(':');
        if (parts.Length != 4)
        {
            error = "Expected format trigger:interval:item:count";
            return false;
        }

        if (!TryParseTrigger(parts[0], out var trigger))
        {
            error = $"Unknown trigger '{parts[0]}'. Use time or kills.";
            return false;
        }

        if (!int.TryParse(parts[1].Trim(), out var interval) || interval < 0)
        {
            error = $"Invalid interval '{parts[1]}'";
            return false;
        }

        var itemName = parts[2].Trim();
        if (string.IsNullOrWhiteSpace(itemName))
        {
            error = "Item name cannot be empty";
            return false;
        }

        if (!int.TryParse(parts[3].Trim(), out var count) || count <= 0)
        {
            error = $"Invalid count '{parts[3]}'";
            return false;
        }

        if (!TryParseOptions(optionParts, trigger, out var optionsError, out var enabled, out var repeatMode, out var cooldownSeconds, out var maxGrants, out var comboTimeSeconds, out var randomTimeSeconds, out var randomKillCount, out var randomAllowTime, out var randomAllowKills, out var randomAllowNewStage, out var randomAllowBossKill))
        {
            error = optionsError;
            return false;
        }

        if ((trigger == RewardTrigger.Time || trigger == RewardTrigger.Kills) && interval <= 0)
        {
            error = $"Invalid interval '{parts[1]}'";
            return false;
        }

        rule = new RewardRule(
            enabled,
            trigger,
            interval,
            itemName,
            count,
            repeatMode,
            cooldownSeconds,
            maxGrants,
            comboTimeSeconds,
            randomTimeSeconds,
            randomKillCount,
            randomAllowTime,
            randomAllowKills,
            randomAllowNewStage,
            randomAllowBossKill);
        return true;
    }

    public override string ToString()
    {
        var unit = Trigger switch
        {
            RewardTrigger.Time => "seconds",
            RewardTrigger.Kills => "kills",
            RewardTrigger.NewStage => "stage",
            RewardTrigger.BossKill => "boss",
            RewardTrigger.Random => "random",
            RewardTrigger.Gold => "gold",
            RewardTrigger.Level => "level",
            _ => string.Empty
        };
        return $"{Trigger}:{Interval} {unit}:{ItemName}:{Count}:{Enabled}";
    }

    private static bool TryParseTrigger(string value, out RewardTrigger trigger)
    {
        switch (Normalize(value))
        {
            case "time":
            case "timer":
            case "second":
            case "seconds":
            case "sec":
            case "secs":
                trigger = RewardTrigger.Time;
                return true;

            case "kill":
            case "kills":
            case "enemy":
            case "enemies":
                trigger = RewardTrigger.Kills;
                return true;

            case "stage":
            case "newstage":
            case "nextstage":
                trigger = RewardTrigger.NewStage;
                return true;

            case "boss":
            case "bosskill":
            case "bosskills":
                trigger = RewardTrigger.BossKill;
                return true;

            case "random":
            case "rand":
                trigger = RewardTrigger.Random;
                return true;

            case "combo":
                trigger = RewardTrigger.Combo;
                return true;

            case "health":
            case "hp":
                trigger = RewardTrigger.Health;
                return true;

            case "gold":
            case "coins":
            case "money":
                trigger = RewardTrigger.Gold;
                return true;

            case "level":
            case "lvl":
                trigger = RewardTrigger.Level;
                return true;

            default:
                trigger = default;
                return false;
        }
    }

    private static bool TryParseOptions(
        string[] optionParts,
        RewardTrigger trigger,
        out string error,
        out bool enabled,
        out RuleRepeatMode repeatMode,
        out int cooldownSeconds,
        out int maxGrants,
        out int comboTimeSeconds,
        out int randomTimeSeconds,
        out int randomKillCount,
        out bool randomAllowTime,
        out bool randomAllowKills,
        out bool randomAllowNewStage,
        out bool randomAllowBossKill)
    {
        error = null;
        enabled = true;
        repeatMode = RuleRepeatMode.Repeat;
        cooldownSeconds = 0;
        maxGrants = 0;
        comboTimeSeconds = 5;
        randomTimeSeconds = 30;
        randomKillCount = 100;
        randomAllowTime = true;
        randomAllowKills = true;
        randomAllowNewStage = true;
        randomAllowBossKill = true;

        if (optionParts.Length == 1)
            return true;

        for (var i = 1; i < optionParts.Length; i++)
        {
            var option = optionParts[i].Trim();
            if (string.IsNullOrWhiteSpace(option))
                continue;

            var keyValue = option.Split('=');
            if (keyValue.Length != 2)
            {
                error = $"Invalid option '{option}'";
                return false;
            }

            var key = Normalize(keyValue[0]);
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "enabled":
                case "e":
                    enabled = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "mode":
                case "repeat":
                case "m":
                    if (!TryParseMode(value, out repeatMode))
                    {
                        error = $"Invalid mode '{value}'";
                        return false;
                    }
                    break;

                case "cooldown":
                case "cd":
                    if (!int.TryParse(value, out cooldownSeconds) || cooldownSeconds < 0)
                    {
                        error = $"Invalid cooldown '{value}'";
                        return false;
                    }
                    break;

                case "max":
                case "limit":
                    if (!int.TryParse(value, out maxGrants) || maxGrants < 0)
                    {
                        error = $"Invalid max grants '{value}'";
                        return false;
                    }
                    break;

                case "ctime":
                case "combotime":
                    if (!int.TryParse(value, out comboTimeSeconds) || comboTimeSeconds <= 0)
                    {
                        error = $"Invalid combo time '{value}'";
                        return false;
                    }
                    break;

                case "rtime":
                    if (!int.TryParse(value, out randomTimeSeconds) || randomTimeSeconds <= 0)
                    {
                        error = $"Invalid random time '{value}'";
                        return false;
                    }
                    break;

                case "rkills":
                    if (!int.TryParse(value, out randomKillCount) || randomKillCount <= 0)
                    {
                        error = $"Invalid random kills '{value}'";
                        return false;
                    }
                    break;

                case "rstage":
                    randomAllowNewStage = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "rboss":
                    randomAllowBossKill = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "rallowtime":
                    randomAllowTime = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "rallowkills":
                    randomAllowKills = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;

                default:
                    error = $"Unknown option '{option}'";
                    return false;
            }
        }

        if (trigger == RewardTrigger.Random && !randomAllowTime && !randomAllowKills && !randomAllowNewStage && !randomAllowBossKill)
        {
            error = "Random trigger needs at least one enabled sub-trigger";
            return false;
        }

        if (repeatMode == RuleRepeatMode.Cooldown && cooldownSeconds <= 0)
        {
            error = "Cooldown mode requires cooldown > 0";
            return false;
        }

        return true;
    }

    private static bool TryParseMode(string value, out RuleRepeatMode repeatMode)
    {
        switch (Normalize(value))
        {
            case "repeat":
            case "loop":
            case "repeating":
                repeatMode = RuleRepeatMode.Repeat;
                return true;
            case "oneshot":
            case "once":
            case "single":
                repeatMode = RuleRepeatMode.OneShot;
                return true;
            case "cooldown":
            case "cd":
                repeatMode = RuleRepeatMode.Cooldown;
                return true;
            default:
                repeatMode = default;
                return false;
        }
    }

    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }
}
