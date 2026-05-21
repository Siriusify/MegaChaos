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
                var playerStats  = GameReflection.GetMember(inventory, "playerStats");

                if (statInv == null) { NotificationService.Show("One Hit KO failed.", null, NotificationService.NotificationType.Unlucky); return; }

                var eStatType = GameReflection.FindType("Il2CppAssets.Scripts.Menu.Shop.EStat", "Assets.Scripts.Menu.Shop.EStat", "EStat");
                var eStatModifyType = GameReflection.FindType("Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType", "Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType", "EStatModifyType");
                var statModifierType = GameReflection.FindType("Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "StatModifier");

                if (eStatType == null || eStatModifyType == null || statModifierType == null) return;
                
                object modType  = Enum.Parse(eStatModifyType, "Multiplication");

                // 1. MaxHealth = 0.0001x (Drops max HP to 1)
                var modHp = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(modHp, "stat", Enum.Parse(eStatType, "MaxHealth"));
                GameReflection.SetMember(modHp, "modifyType", modType);
                GameReflection.SetMember(modHp, "modification", 0.0001f);
                GameReflection.InvokeInstance(statInv, "ChangeStat", new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) }, modHp, false, DefaultDuration, false);

                // 2. DamageMultiplier = 9999x (Player one-shots mobs)
                var modDmg = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(modDmg, "stat", Enum.Parse(eStatType, "DamageMultiplier"));
                GameReflection.SetMember(modDmg, "modifyType", modType);
                GameReflection.SetMember(modDmg, "modification", 99999f);
                GameReflection.InvokeInstance(statInv, "ChangeStat", new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) }, modDmg, false, DefaultDuration, false);

                // 3. EnemyDamageMultiplier = 9999x (Mobs one-shot player if shield exists, etc.)
                var modEDmg = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(modEDmg, "stat", Enum.Parse(eStatType, "EnemyDamageMultiplier"));
                GameReflection.SetMember(modEDmg, "modifyType", modType);
                GameReflection.SetMember(modEDmg, "modification", 99999f);
                GameReflection.InvokeInstance(statInv, "ChangeStat", new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) }, modEDmg, false, DefaultDuration, false);

                if (playerStats != null)
                    GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);

                NotificationService.Show("ONE HIT KO! Kill or be killed! 😈", null, NotificationService.NotificationType.Warning);
            }
            catch (Exception ex) { MegaChaos.Main.Error("[OneHitKO] OnStart: " + ex.Message); }
        }

        public void OnUpdate(float dt) { }
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
