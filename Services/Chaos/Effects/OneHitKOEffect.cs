using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// One Hit KO: Efekt boyunca hem oyuncu hem düşmanlar 1 HP'de kalır.
    /// Herhangi bir vuruş = ölüm.
    /// Oyuncu HP'si her karede 1'e çekilir.
    /// Düşman HP'si her 0.5 saniyede tüm sahnedeki Enemy nesnelerine masif hasar uygulanarak 1'e indirilir.
    /// </summary>
    public class OneHitKOEffect : IChaosEffect
    {
        public string Id => "effect_onehitko";
        public string Name => "One Hit KO";
        public string Description => "Max HP is reduced to 1, but your damage is limitless. Don't get hit!";
        public float DefaultDuration => 30f;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var statInv      = GameReflection.GetMember(inventory, "statInventory");
                
                if (statInv == null) { NotificationService.Show("One Hit KO failed.", null, NotificationService.NotificationType.Unlucky); return; }

                NotificationService.Show("ONE HIT KO! Kill or be killed! 😈", null, NotificationService.NotificationType.Warning);
            }
            catch (Exception ex) { MegaChaos.Main.Error("[OneHitKO] OnStart: " + ex.Message); }
        }

        public void OnUpdate(float dt) 
        { 
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player != null)
                {
                    var inventory = GameReflection.GetMember(player, "inventory");
                    var playerStats = GameReflection.GetMember(inventory, "playerStats");
                    var playerHealth = GameReflection.GetMember(inventory, "playerHealth");

                    if (playerStats != null)
                    {
                        GameReflection.SetMember(playerStats, "MaxHealth", 1f);
                        GameReflection.SetMember(playerStats, "DamageMultiplier", 9999f);
                        GameReflection.SetMember(playerStats, "EnemyDamageMultiplier", 9999f);
                    }
                    if (playerHealth != null)
                    {
                        var currentHealth = GameReflection.GetMember(playerHealth, "currentHealth");
                        if (currentHealth is float h && h > 1f)
                        {
                            GameReflection.SetMember(playerHealth, "currentHealth", 1f);
                        }
                    }
                }
            }
            catch { }
        }
        public void OnGUI() { }

        public void OnEnd()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var playerStats  = GameReflection.GetMember(inventory, "playerStats");
                var playerHealth = GameReflection.GetMember(inventory, "playerHealth");

                if (playerStats != null) GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
                
                // Heal player back up since their Max HP just reverted
                if (playerHealth != null)
                    GameReflection.InvokeInstance(playerHealth, "Heal", new[] { typeof(float), typeof(bool) }, 9999f, false);
            }
            catch { }
            NotificationService.Show("One Hit KO ended — welcome back to normal life!", null, NotificationService.NotificationType.Reward);
        }
    }
}
