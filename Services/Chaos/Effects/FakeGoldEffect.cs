using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Tax Audit — Seizes gold (10-9999) and excess item stacks, returns them after 5 seconds.
    /// </summary>
    public class FakeGoldEffect : IChaosEffect
    {
        public string Id => "effect_fakegold";
        public string Name => "Tax Audit";
        public string Description => "Gives you some gold... but it's fake! It will be taken back.";
        public float DefaultDuration => 5f;

        private int _goldSeized;
        private readonly Dictionary<object, int> _seizedItems = new();
        private object _playerInventory;
        private object _itemInventory;
        private Type _eItemType;

        public void OnStart()
        {
            _goldSeized = 0;
            _seizedItems.Clear();
            _playerInventory = null;
            _itemInventory = null;

            try
            {
                // --- Resolve player references ---
                var myPlayerType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                    "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                _playerInventory = GameReflection.GetMember(player, "inventory");
                _itemInventory   = GameReflection.GetMember(_playerInventory, "itemInventory");
                _eItemType       = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                    "Assets.Scripts.Inventory__Items__Pickups.Items.EItem", "EItem");

                // --- Seize gold ---
                int currentGold = RunStatService.GetGold();
                int level = RunStatService.GetLevel();
                if (level < 1) level = 1;

                int minAmount = 10 + ((level - 1) * 20);
                int maxAmount = 50 + (level * 50);
                int target = UnityEngine.Random.Range(minAmount, maxAmount);
                _goldSeized = Math.Min(currentGold, target);
                if (_goldSeized > 0)
                    GameReflection.InvokeInstance(_playerInventory, "ChangeGold", new[] { typeof(int) }, -_goldSeized);

                int remainingDebt = target - _goldSeized; // how much gold was still owed after taking all gold

                // --- Seize excess item stacks (counts > 1, take all but 1) ---
                // Only triggered when we couldn't cover the gold amount entirely
                if (remainingDebt > 0 && _eItemType != null)
                {
                    foreach (var enumVal in Enum.GetValues(_eItemType))
                    {
                        try
                        {
                            var countObj = GameReflection.InvokeInstance(_itemInventory, "GetAmount", new[] { _eItemType }, enumVal);
                            if (countObj == null) continue;
                            int count = Convert.ToInt32(countObj);
                            if (count <= 1) continue; // keep at least 1

                            int toSeize = count - 1; // take all but 1
                            _seizedItems[enumVal] = toSeize;
                            for (int i = 0; i < toSeize; i++)
                                GameReflection.InvokeInstance(_itemInventory, "RemoveItem", new[] { _eItemType, typeof(bool) }, enumVal, false);
                        }
                        catch { }
                    }
                }

                int itemTypesStolenCount = _seizedItems.Count;
                string msg = $"GOLD HEIST! Tax Audit: {target}G.";
                if (_goldSeized > 0) msg += $" Took {_goldSeized}G.";
                if (remainingDebt > 0)
                {
                    msg += $" Short by {remainingDebt}G — seized items to cover it.";
                }
                if (itemTypesStolenCount > 0) msg += $" Confiscated {itemTypesStolenCount} item types!";
                else if (_goldSeized == 0) msg += " Nothing to steal...";

                NotificationService.Show(msg, null, NotificationService.NotificationType.Unlucky);
                Main.Msg($"[TaxAudit] Seized {_goldSeized} gold, {_seizedItems.Count} item types. (fake, returns on end)");
            }
            catch (Exception ex)
            {
                Main.Error("[TaxAudit] OnStart: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            try
            {
                // --- Return gold ---
                if (_playerInventory != null && _goldSeized > 0)
                    GameReflection.InvokeInstance(_playerInventory, "ChangeGold", new[] { typeof(int) }, _goldSeized);

                // --- Return seized items ---
                if (_itemInventory != null && _eItemType != null)
                {
                    foreach (var pair in _seizedItems)
                        GameReflection.InvokeInstance(_itemInventory, "AddItem", new[] { _eItemType, typeof(int) }, pair.Key, pair.Value);
                }

                _seizedItems.Clear();
                NotificationService.Show("Just kidding! Gold & items returned.", null, NotificationService.NotificationType.Reward);
                ChaosEngine.Instance.AddLogEntry("Tax Audit (It was fake!)");
                Main.Msg("[TaxAudit] All assets returned.");
            }
            catch (Exception ex)
            {
                Main.Error("[TaxAudit] OnEnd: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
