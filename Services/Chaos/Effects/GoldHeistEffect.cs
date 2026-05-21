using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Gold Heist — Permanently takes 10–9999 gold from the player.
    /// If there's not enough gold, takes all gold AND excess item stacks (all but 1 per type).
    /// Nothing is returned.
    /// </summary>
    public class GoldHeistEffect : IChaosEffect
    {
        public string Id => "effect_goldheist";
        public string Name => "Tax Audit";
        public string Description => "All your gold is stolen!";
        public float DefaultDuration => 0f; // instant — nothing to undo

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                    "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var playerInventory = GameReflection.GetMember(player, "inventory");
                var itemInventory   = GameReflection.GetMember(playerInventory, "itemInventory");
                var eItemType       = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                    "Assets.Scripts.Inventory__Items__Pickups.Items.EItem", "EItem");

                int currentGold = RunStatService.GetGold();
                int target = UnityEngine.Random.Range(10, 10000); // 10–9999

                int goldTaken = Math.Min(currentGold, target);
                if (goldTaken > 0)
                    GameReflection.InvokeInstance(playerInventory, "ChangeGold", new[] { typeof(int) }, -goldTaken);

                int itemTypesStolenCount = 0;
                int remainingDebt = target - goldTaken;

                // If gold wasn't enough, also steal excess items
                if (remainingDebt > 0 && eItemType != null)
                {
                    foreach (var enumVal in Enum.GetValues(eItemType))
                    {
                        try
                        {
                            var countObj = GameReflection.InvokeInstance(itemInventory, "GetAmount", new[] { eItemType }, enumVal);
                            if (countObj == null) continue;
                            int count = Convert.ToInt32(countObj);
                            if (count <= 1) continue; // always leave 1

                            int toSteal = count - 1;
                            for (int i = 0; i < toSteal; i++)
                                GameReflection.InvokeInstance(itemInventory, "RemoveItem", new[] { eItemType, typeof(bool) }, enumVal, false);
                            itemTypesStolenCount++;
                        }
                        catch { }
                    }
                }

                string msg = $"GOLD HEIST! Tax Audit: {target}G.";
                if (goldTaken > 0) msg += $" Took {goldTaken}G.";
                if (itemTypesStolenCount > 0) msg += $" Confiscated {itemTypesStolenCount} items!";
                else if (goldTaken == 0) msg += " Nothing to steal...";

                NotificationService.Show(msg, null, NotificationService.NotificationType.Unlucky);
                Main.Msg($"[GoldHeist] Target: {target}, Took {goldTaken} gold, {itemTypesStolenCount} item types stolen.");
            }
            catch (Exception ex)
            {
                Main.Error("[GoldHeist] OnStart: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { } // permanent, nothing to restore
    }
}
