using System;
using System.Collections.Concurrent;

namespace MegaChaos.Services;

internal static class ItemGrantService
{
    private static readonly ConcurrentDictionary<string, object> ItemCache = new();
    private static readonly ConcurrentDictionary<string, byte> InvalidItemCache = new();
    private static Type _eItemType;
    private static readonly Random _rng = new();
    private static readonly string[] UnluckyMessages = {
        "Unlucky! You got nothing.",
        "Better luck next time...",
        "The gods are not smiling upon you.",
        "A swing and a miss! No item.",
        "Nothing dropped! RNG hates you.",
        "Empty handed this time."
    };
    private static string _lastWarning;

    public static bool GrantItem(string itemName, int count)
    {
        if (string.IsNullOrWhiteSpace(itemName) || itemName.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            var msg = UnluckyMessages[_rng.Next(UnluckyMessages.Length)];
            NotificationService.Show(msg, null, NotificationService.NotificationType.Unlucky);
            return true;
        }

        if (count <= 0)
            return false;

        if (TryGrantViaInventory(itemName, count))
        {
            Main.Msg($"Granted {count}x {itemName}");
            NotificationService.Show($"Granted {count}x {itemName}", itemName);
            return true;
        }

        if (TryGrantViaItemManager(itemName, count))
        {
            Main.Msg($"Granted {count}x {itemName}");
            NotificationService.Show($"Granted {count}x {itemName}", itemName);
            return true;
        }

        WarnOnce($"Could not grant item '{itemName}'. No compatible item grant path is available.");
        return false;
    }

    /// <summary>Clears the invalid-item cache so renamed/corrected items are retried on next grant.</summary>
    public static void ClearInvalidCache() => InvalidItemCache.Clear();

    private static bool TryGrantViaInventory(string itemName, int count)
    {
        var item = ResolveItem(itemName);
        if (item == null || _eItemType == null)
            return false;

        var itemInventory = GetItemInventory();
        if (itemInventory == null)
            return false;

        try
        {
            GameReflection.InvokeInstance(itemInventory, "AddItem", new[] { _eItemType, typeof(int) }, item, count);
            return true;
        }
        catch (Exception ex)
        {
            Main.Warn($"Inventory AddItem failed for '{itemName}': {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static bool TryGrantViaItemManager(string itemName, int count)
    {
        try
        {
            var itemManagerType = GameReflection.FindType(
                "Il2CppAssets.Scripts.Managers.ItemManager",
                "Assets.Scripts.Managers.ItemManager",
                "ItemManager");

            var instance = GameReflection.GetStaticMember(itemManagerType, "Instance");
            if (instance == null)
                return false;

            GameReflection.InvokeInstance(instance, "GrantItem", new[] { typeof(string), typeof(int) }, itemName, count);
            return true;
        }
        catch (Exception ex)
        {
            Main.Warn($"ItemManager GrantItem failed for '{itemName}': {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static object ResolveItem(string itemName)
    {
        var cacheKey = RewardRule.Normalize(itemName);
        if (InvalidItemCache.ContainsKey(cacheKey))
            return null;

        if (ItemCache.TryGetValue(cacheKey, out var cached))
            return cached;

        _eItemType ??= GameReflection.FindType(
            "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
            "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
            "EItem");

        if (_eItemType == null)
            return null;

        foreach (var enumName in Enum.GetNames(_eItemType))
        {
            if (RewardRule.Normalize(enumName) != cacheKey)
                continue;

            var value = Enum.Parse(_eItemType, enumName);
            ItemCache[cacheKey] = value;
            return value;
        }

        InvalidItemCache.TryAdd(cacheKey, 0);
        return null;
    }

    private static object GetItemInventory()
    {
        var myPlayerType = GameReflection.FindType(
            "Il2CppAssets.Scripts.Actors.Player.MyPlayer",
            "Assets.Scripts.Actors.Player.MyPlayer",
            "MyPlayer");

        var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
        var playerInventory = GameReflection.GetMember(player, "inventory");
        return GameReflection.GetMember(playerInventory, "itemInventory");
    }

    private static void WarnOnce(string message)
    {
        if (_lastWarning == message)
            return;

        _lastWarning = message;
        Main.Warn(message);
    }
}
