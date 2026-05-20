using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// StatInventory.ChangeStat → İstatistik değişir.
    /// PlayerStatsNew.ForceUpdateStats → UI hemen yenilenir, Stats penceresinde görünür.
    /// </summary>
    public class RandomStatEffect : IChaosEffect
    {
        public string Id => "effect_randomstat";
        public string Name => "Stat Lottery";
        public string Description => "Rastgele bir stat geçici olarak değişir — Stats penceresinde görünür!";
        public float DefaultDuration => 30f;

        private static readonly System.Random _rng = new();

        // Oyuncu için anlamlı stat'lar (Unused0, EnemyX gibi garip olanları çıkardık)
        private static readonly string[] _usefulStats =
        {
            "MaxHealth", "HealthRegen", "Shield", "Armor", "Evasion",
            "DamageMultiplier", "AttackSpeed", "Projectiles", "CritChance", "CritDamage",
            "MoveSpeedMultiplier", "JumpHeight", "DurationMultiplier", "Lifesteal",
            "KnockbackMultiplier", "Luck", "ProjectileSpeedMultiplier", "FireDamage",
            "IceDamage", "LightningDamage", "HealingMultiplier", "Overheal"
        };

        private bool _applied;

        public void OnStart()
        {
            _applied = false;
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var statInv      = GameReflection.GetMember(inventory, "statInventory");
                var playerStats  = GameReflection.GetMember(inventory, "playerStats"); // PlayerStatsNew

                if (statInv == null) { NotificationService.Show("Stat system not found.", null, NotificationService.NotificationType.Unlucky); return; }

                var eStatType = GameReflection.FindType("Il2CppAssets.Scripts.Menu.Shop.EStat", "Assets.Scripts.Menu.Shop.EStat", "EStat");
                var eStatModifyType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType", "EStatModifyType");
                var statModifierType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.StatModifier",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "StatModifier");

                if (eStatType == null || eStatModifyType == null || statModifierType == null)
                {
                    MegaChaos.Main.Warn("[RandomStat] Tip bulunamadı.");
                    return;
                }

                // Rastgele anlamlı bir stat seç
                string statName  = _usefulStats[_rng.Next(_usefulStats.Length)];
                object chosenStat = Enum.Parse(eStatType, statName);

                bool buff       = _rng.NextDouble() > 0.40;
                float modAmount = buff
                    ? (float)(1.5 + _rng.NextDouble() * 1.0)   // x1.5 – x2.5
                    : (float)(0.2 + _rng.NextDouble() * 0.4);  // x0.2 – x0.6
                object modType  = Enum.Parse(eStatModifyType, "Multiplication");

                // StatModifier oluştur
                var modifier = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(modifier, "stat",         chosenStat);
                GameReflection.SetMember(modifier, "modifyType",   modType);
                GameReflection.SetMember(modifier, "modification", modAmount);

                // ChangeStat: permanent=false, timeout=DefaultDuration, addToShrineLog=false
                GameReflection.InvokeInstance(statInv, "ChangeStat",
                    new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                    modifier, false, DefaultDuration, false);

                // Stats penceresini hemen güncelle (ForceUpdateStats)
                if (playerStats != null)
                    GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);

                _applied = true;
                string dir = buff ? $"↑ BUFF (x{modAmount:F2})" : $"↓ NERF (x{modAmount:F2})";
                NotificationService.Show($"{statName}: {dir} (Stats'tan bakabilirsin!)", null,
                    buff ? NotificationService.NotificationType.Reward : NotificationService.NotificationType.Unlucky);
                MegaChaos.Main.Msg($"[RandomStat] {statName} {dir} — timeout={DefaultDuration}s");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[RandomStat] " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            // Timeout ile ChangeStat otomatik kaldırılıyor.
            // Ama ForceUpdateStats çağırarak UI'ı senkronize et.
            try
            {
                if (_applied)
                {
                    var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                    var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                    var inventory    = GameReflection.GetMember(player, "inventory");
                    var playerStats  = GameReflection.GetMember(inventory, "playerStats");
                    if (playerStats != null)
                        GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
                    NotificationService.Show("Stat back to normal.", null, NotificationService.NotificationType.Reward);
                }
            }
            catch { }
        }
    }
}
