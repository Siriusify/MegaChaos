using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Rastgele 1-3 item verir VEYA envanterden rastgele 1-3 item alır.
    /// </summary>
    public class RandomItemEffect : IChaosEffect
    {
        public string Id => "effect_randomitem";
        public string Name => "Item Lottery";
        public string Description => "Rastgele item alırsın ya da kaybedersin — şansa bak!";
        public float DefaultDuration => 0f; // anlık

        private static readonly System.Random _rng = new();

        public void OnStart()
        {
            var eItemType = GameReflection.FindType(
                "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
                "EItem");
            if (eItemType == null) { MegaChaos.Main.Warn("[RandomItem] EItem type not found."); return; }

            var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
            var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
            var inventory    = GameReflection.GetMember(player, "inventory");
            var itemInv      = GameReflection.GetMember(inventory, "itemInventory");
            if (itemInv == null) return;

            var allItems = Enum.GetValues(eItemType);
            bool give = _rng.NextDouble() > 0.45; // %55 ihtimalle ver, %45 al
            int count = _rng.Next(1, 4);

            if (give)
            {
                // Rastgele item ver
                var pick = allItems.GetValue(_rng.Next(allItems.Length));
                for (int i = 0; i < count; i++)
                    GameReflection.InvokeInstance(itemInv, "AddItem", new[] { eItemType, typeof(int) }, pick, 1);
                NotificationService.Show($"+{count}x {pick} (Çekiliş Kazandın!)", null, NotificationService.NotificationType.Reward);
            }
            else
            {
                // Envanterden rastgele sahip olunan itemi al
                var owned = new List<object>();
                foreach (var v in allItems)
                {
                    var c = GameReflection.InvokeInstance(itemInv, "GetAmount", new[] { eItemType }, v);
                    if (c != null && Convert.ToInt32(c) > 0) owned.Add(v);
                }
                if (owned.Count == 0) { NotificationService.Show("Çekiliş: Alınacak item yok!", null, NotificationService.NotificationType.Unlucky); return; }
                var pick = owned[_rng.Next(owned.Count)];
                int removeCount = Math.Min(count, Convert.ToInt32(GameReflection.InvokeInstance(itemInv, "GetAmount", new[] { eItemType }, pick)));
                for (int i = 0; i < removeCount; i++)
                    GameReflection.InvokeInstance(itemInv, "RemoveItem", new[] { eItemType, typeof(bool) }, pick, false);
                NotificationService.Show($"-{removeCount}x {pick} (Çekiliş Kaybettin!)", null, NotificationService.NotificationType.Unlucky);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
