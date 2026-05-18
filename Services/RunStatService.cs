using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace MegaChaos.Services;

internal static class RunStatService
{
    private static readonly ConcurrentDictionary<string, object> StatCache = new();
    private static readonly ConcurrentDictionary<string, byte> MissingStatCache = new();
    private static Type _runStatsType;
    private static Type _eMyStatType;
    private static bool _missingTypesLogged;

    public static int GetKills()
    {
        return GetStatValue("kills");
    }

    public static int GetBossKills()
    {
        return GetStatValue("bossKills");
    }

    private static bool _healthLoggedOnce;
    private static int _healthCallCount;

    public static float? GetHealthPercentage()
    {
        _healthCallCount++;
        try
        {
            var gameManagerType = GameReflection.FindType("GameManager", "Il2CppGameManager");
            if (gameManagerType == null)
                return null;

            var gameManagerInstance = GameReflection.InvokeStatic(gameManagerType, "get_Instance", Type.EmptyTypes);
            if (gameManagerInstance == null)
            {
                if (_healthCallCount == 1)
                    Main.Msg("GetHealthPercentage: GameManager.Instance is null (game not started)");
                return null;
            }

            var myPlayer = GameReflection.GetMember(gameManagerInstance, "player");
            if (myPlayer == null)
                return null;

            var inventory = GameReflection.GetMember(myPlayer, "inventory");
            if (inventory == null)
                return null;

            var playerHealth = GameReflection.GetMember(inventory, "playerHealth");
            if (playerHealth == null)
                return null;

            var hp = GameReflection.GetMember(playerHealth, "hp");
            var maxHp = GameReflection.GetMember(playerHealth, "maxHp");
            
            if (hp == null || maxHp == null)
            {
                if (!_healthLoggedOnce)
                {
                    Main.Warn($"GetHealthPercentage: hp/maxHp not found. Dumping members:");
                    var t = playerHealth.GetType();
                    var members = string.Join(", ", t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(m => m.Name).Take(30));
                    Main.Warn($"Members: {members}");
                    _healthLoggedOnce = true;
                }
                return null;
            }

            var hpVal = Convert.ToSingle(hp);
            var maxHpVal = Convert.ToSingle(maxHp);
            if (maxHpVal > 0) 
            {
                var result = hpVal / maxHpVal * 100f;
                if (_healthCallCount <= 5 || _healthCallCount % 100 == 0)
                    Main.Msg($"GetHealthPercentage: {hpVal:F1}/{maxHpVal:F1} = {result:F1}%");
                return result;
            }
        }
        catch (Exception ex) 
        {
            if (!_healthLoggedOnce)
            {
                Main.Error($"GetHealthPercentage: {ex.GetBaseException().Message}");
                _healthLoggedOnce = true;
            }
        }
        return null;
    }

    private static bool _goldLoggedOnce;
    private static int _goldCallCount;

    public static int GetGold()
    {
        _goldCallCount++;
        try
        {
            var gameManagerType = GameReflection.FindType("GameManager", "Il2CppGameManager");
            if (gameManagerType == null)
                return 0;

            var gameManagerInstance = GameReflection.InvokeStatic(gameManagerType, "get_Instance", Type.EmptyTypes);
            if (gameManagerInstance == null)
            {
                if (_goldCallCount == 1)
                    Main.Msg("GetGold: GameManager.Instance is null (game not started)");
                return 0;
            }

            var myPlayer = GameReflection.GetMember(gameManagerInstance, "player");
            if (myPlayer == null)
                return 0;

            var inventory = GameReflection.GetMember(myPlayer, "inventory");
            if (inventory == null)
                return 0;

            var goldValue = GameReflection.GetMember(inventory, "gold") 
                ?? GameReflection.GetMember(inventory, "coins")
                ?? GameReflection.GetMember(inventory, "currency")
                ?? GameReflection.GetMember(inventory, "money")
                ?? GameReflection.GetMember(inventory, "materials");
            
            if (goldValue != null)
            {
                var result = Convert.ToInt32(goldValue);
                if (_goldCallCount <= 5 || _goldCallCount % 100 == 0)
                    Main.Msg($"GetGold: {result}");
                return result;
            }

            if (!_goldLoggedOnce)
            {
                Main.Warn($"GetGold: Currency not found on PlayerInventory. Dumping members:");
                var t = inventory.GetType();
                var members = string.Join(", ", t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(m => m.Name).Take(50));
                Main.Warn($"Members: {members}");
                _goldLoggedOnce = true;
            }

            return 0;
        }
        catch (Exception ex) 
        {
            if (!_goldLoggedOnce)
            {
                Main.Error($"GetGold: {ex.GetBaseException().Message}");
                _goldLoggedOnce = true;
            }
        }

        return 0;
    }

    public static int GetLevel()
    {
        try
        {
            var gameManagerType = GameReflection.FindType("GameManager", "Il2CppGameManager");
            if (gameManagerType == null)
                return 0;

            var gameManagerInstance = GameReflection.InvokeStatic(gameManagerType, "get_Instance", Type.EmptyTypes);
            if (gameManagerInstance == null)
                return 0;

            var myPlayer = GameReflection.GetMember(gameManagerInstance, "player");
            if (myPlayer == null)
                return 0;

            var inventory = GameReflection.GetMember(myPlayer, "inventory");
            if (inventory == null)
                return 0;

            var levelValue = GameReflection.GetMember(inventory, "level") 
                ?? GameReflection.GetMember(inventory, "playerLevel")
                ?? GameReflection.GetMember(inventory, "currentLevel");
            
            if (levelValue != null)
                return Convert.ToInt32(levelValue);

            return 0;
        }
        catch { }

        return 0;
    }

    public static int? GetCurrentCombo()
    {
        var candidates = new[] { "combo", "currentCombo", "killCombo", "comboCount", "currentKillCombo" };

        foreach (var candidate in candidates)
        {
            var stat = ResolveStat(candidate, false);
            if (stat == null)
                continue;

            try
            {
                var value = GameReflection.InvokeStatic(_runStatsType, "GetStat", new[] { _eMyStatType }, stat);
                return value == null ? 0 : Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public static int GetStatValue(string statName)
    {
        var stat = ResolveStat(statName, true);
        if (stat == null)
            return 0;

        try
        {
            var value = GameReflection.InvokeStatic(_runStatsType, "GetStat", new[] { _eMyStatType }, stat);
            return value == null ? 0 : Convert.ToInt32(value);
        }
        catch (Exception ex)
        {
            Main.Error($"Failed to read stat '{statName}': {ex.GetBaseException().Message}");
            return 0;
        }
    }

    private static object ResolveStat(string statName, bool logIfMissing)
    {
        var cacheKey = RewardRule.Normalize(statName);
        if (StatCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (!logIfMissing && MissingStatCache.ContainsKey(cacheKey))
            return null;

        _runStatsType ??= GameReflection.FindType(
            "Il2CppAssets.Scripts.Saves___Serialization.Progression.Stats.RunStats",
            "Assets.Scripts.Saves___Serialization.Progression.Stats.RunStats",
            "RunStats");

        _eMyStatType ??= GameReflection.FindType(
            "Il2CppAssets.Scripts.Saves___Serialization.Progression.Stats.EMyStat",
            "Assets.Scripts.Saves___Serialization.Progression.Stats.EMyStat",
            "EMyStat");

        if (_runStatsType == null || _eMyStatType == null)
        {
            if (!_missingTypesLogged)
            {
                _missingTypesLogged = true;
                Main.Warn("Could not find RunStats or EMyStat. Kill rules disabled until loaded.");
            }
            return null;
        }

        foreach (var enumName in Enum.GetNames(_eMyStatType))
        {
            if (RewardRule.Normalize(enumName) != cacheKey)
                continue;

            var value = Enum.Parse(_eMyStatType, enumName);
            StatCache[cacheKey] = value;
            return value;
        }

        if (logIfMissing)
            Main.Error($"Could not find EMyStat.{statName}.");
        else
            MissingStatCache.TryAdd(cacheKey, 0);
        return null;
    }
}
