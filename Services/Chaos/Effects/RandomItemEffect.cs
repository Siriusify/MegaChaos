using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Item Lottery — Randomly gives 1-3 items OR takes 1-3 items.
    /// Both outcomes are real and permanent.
    /// </summary>
    public class RandomItemEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_randomitem";
        public string Name => _displayName;
        public string Description => "Win or lose random items — luck decides!";
        public float DefaultDuration => 0f;

        private string _displayName = "Item Lottery";

        public bool HideProgressBar => true;

        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        protected virtual bool ForceGive => false;
        protected virtual bool ForceTake => false;

        protected static readonly System.Random Rng = new();

        public virtual void OnStart()
        {
            Execute(returnOnEnd: false);
        }

        protected (object picked, int count, bool gave) Execute(bool returnOnEnd)
        {
            var eItemType = GameReflection.FindType(
                "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "Assets.Scripts.Inventory__Items__Pickups.Items.EItem", "EItem");
            if (eItemType == null) return (null, 0, false);

            var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
            var player    = GameReflection.GetStaticMember(myPlayerType, "Instance");
            var inventory = GameReflection.GetMember(player, "inventory");
            var itemInv   = GameReflection.GetMember(inventory, "itemInventory");
            if (itemInv == null) return (null, 0, false);

            bool give = ForceGive || (!ForceTake && Rng.NextDouble() > 0.45);
            int count = Rng.Next(1, 4);
            var allItems = Enum.GetValues(eItemType);

            if (give)
            {
                var pick = allItems.GetValue(Rng.Next(allItems.Length));
                for (int i = 0; i < count; i++)
                    GameReflection.InvokeInstance(itemInv, "AddItem", new[] { eItemType, typeof(int) }, pick, 1);
                _displayName = $"Item Lottery: +{count}x {pick}";
                NotificationService.Show($"+{count}x {pick} (Item Lottery Win!)", null, NotificationService.NotificationType.Reward);
                return (pick, count, true);
            }
            else
            {
                var owned = new List<object>();
                foreach (var v in allItems)
                {
                    var c = GameReflection.InvokeInstance(itemInv, "GetAmount", new[] { eItemType }, v);
                    if (c != null && Convert.ToInt32(c) > 0) owned.Add(v);
                }
                if (owned.Count == 0)
                {
                    NotificationService.Show("Item Lottery: Nothing to take!", null, NotificationService.NotificationType.Unlucky);
                    return (null, 0, false);
                }
                var pick = owned[Rng.Next(owned.Count)];
                int removeCount = Math.Min(count, Convert.ToInt32(
                    GameReflection.InvokeInstance(itemInv, "GetAmount", new[] { eItemType }, pick)));
                for (int i = 0; i < removeCount; i++)
                    GameReflection.InvokeInstance(itemInv, "RemoveItem", new[] { eItemType, typeof(bool) }, pick, false);
                _displayName = $"Item Lottery: -{removeCount}x {pick}";
                NotificationService.Show($"-{removeCount}x {pick} (Item Lottery Loss!)", null, NotificationService.NotificationType.Unlucky);
                return (pick, removeCount, false);
            }
        }

        public virtual void OnUpdate(float dt) { }
        public virtual void OnGUI() { }
        public virtual void OnEnd() { }
    }

    /// <summary>
    /// Fake Item Lottery — Removes items but returns them after 5 seconds.
    /// If it gave items, takes them back. Name appears the same as real lottery.
    /// </summary>
    public class FakeItemLotteryEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_fakeitemlottery";
        public string Name => _displayName;
        public string Description => "Fake item lottery — what's given or taken is reversed after 5 seconds!";
        public float DefaultDuration => 5f;

        private string _displayName = "Item Lottery";
        public bool HideProgressBar => true;
        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        private static readonly System.Random Rng = new();
        private object _eItemType;
        private object _itemInv;
        private object _pickedItem;
        private int _count;
        private bool _gave;

        public void OnStart()
        {
            _eItemType = GameReflection.FindType(
                "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "Assets.Scripts.Inventory__Items__Pickups.Items.EItem", "EItem");
            if (_eItemType == null) return;

            var eType = (Type)_eItemType;
            var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
            var player    = GameReflection.GetStaticMember(myPlayerType, "Instance");
            var inventory = GameReflection.GetMember(player, "inventory");
            _itemInv = GameReflection.GetMember(inventory, "itemInventory");
            if (_itemInv == null) return;

            bool give = Rng.NextDouble() > 0.45;
            _count = Rng.Next(1, 4);
            var allItems = Enum.GetValues(eType);

            if (give)
            {
                _pickedItem = allItems.GetValue(Rng.Next(allItems.Length));
                _gave = true;
                for (int i = 0; i < _count; i++)
                    GameReflection.InvokeInstance(_itemInv, "AddItem", new[] { eType, typeof(int) }, _pickedItem, 1);
                _displayName = $"Item Lottery: +{_count}x {_pickedItem}";
                NotificationService.Show($"+{_count}x {_pickedItem} (Item Lottery Win!)", null, NotificationService.NotificationType.Reward);
            }
            else
            {
                var owned = new List<object>();
                foreach (var v in Enum.GetValues(eType))
                {
                    var c = GameReflection.InvokeInstance(_itemInv, "GetAmount", new[] { eType }, v);
                    if (c != null && Convert.ToInt32(c) > 0) owned.Add(v);
                }
                if (owned.Count == 0)
                {
                    NotificationService.Show("Item Lottery: Nothing to take!", null, NotificationService.NotificationType.Unlucky);
                    return;
                }
                _pickedItem = owned[Rng.Next(owned.Count)];
                _gave = false;
                int removeCount = Math.Min(_count, Convert.ToInt32(
                    GameReflection.InvokeInstance(_itemInv, "GetAmount", new[] { eType }, _pickedItem)));
                _count = removeCount;
                for (int i = 0; i < removeCount; i++)
                    GameReflection.InvokeInstance(_itemInv, "RemoveItem", new[] { eType, typeof(bool) }, _pickedItem, false);
                _displayName = $"Item Lottery: -{_count}x {_pickedItem}";
                NotificationService.Show($"-{_count}x {_pickedItem} (Item Lottery Loss!)", null, NotificationService.NotificationType.Unlucky);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_pickedItem == null || _itemInv == null || _eItemType == null) return;
            var eType = (Type)_eItemType;
            if (_gave)
            {
                // Gave items → take them back
                for (int i = 0; i < _count; i++)
                    GameReflection.InvokeInstance(_itemInv, "RemoveItem", new[] { eType, typeof(bool) }, _pickedItem, false);
                NotificationService.Show("Item Lottery was fake! Items taken back.", null, NotificationService.NotificationType.Warning);
            }
            else
            {
                // Took items → return them
                GameReflection.InvokeInstance(_itemInv, "AddItem", new[] { eType, typeof(int) }, _pickedItem, _count);
                NotificationService.Show("Item Lottery was fake! Items returned.", null, NotificationService.NotificationType.Reward);
            }
            ChaosEngine.Instance.AddLogEntry("Item Lottery (It was fake!)");
        }
    }
}
