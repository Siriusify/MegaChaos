using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class RandomStatEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_randomstat";
        public string Name => _displayName;
        public string Description => "A random stat is permanently changed!";
        public float DefaultDuration => 0f;

        public bool HideProgressBar => true;

        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        private static readonly System.Random _rng = new();

        private static readonly string[] _usefulStats =
        {
            "MaxHealth", "HealthRegen", "Shield", "Armor", "Evasion",
            "DamageMultiplier", "AttackSpeed", "Projectiles", "CritChance", "CritDamage",
            "MoveSpeedMultiplier", "JumpHeight", "DurationMultiplier", "Lifesteal",
            "KnockbackMultiplier", "Luck", "ProjectileSpeedMultiplier", "FireDamage",
            "IceDamage", "LightningDamage", "HealingMultiplier", "Overheal"
        };

        private string _displayName = "Stat Lottery";

        private string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "???";
            return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        }

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var statInv      = GameReflection.GetMember(inventory, "statInventory");
                var playerStats  = GameReflection.GetMember(inventory, "playerStats");

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

                string rawStatName  = _usefulStats[_rng.Next(_usefulStats.Length)];
                string statName = FormatName(rawStatName);
                object chosenStat = Enum.Parse(eStatType, rawStatName);

                bool buff       = _rng.NextDouble() > 0.40;
                float modAmount = buff
                    ? (float)(1.5 + _rng.NextDouble() * 1.0)
                    : (float)(0.2 + _rng.NextDouble() * 0.4);
                object modType  = Enum.Parse(eStatModifyType, "Multiplication");

                var modifier = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(modifier, "stat",         chosenStat);
                GameReflection.SetMember(modifier, "modifyType",   modType);
                GameReflection.SetMember(modifier, "modification", modAmount);

                GameReflection.InvokeInstance(statInv, "ChangeStat",
                    new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                    modifier, true, 0f, false);

                if (playerStats != null)
                    GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
                string dir = buff ? $"↑ BUFF (x{modAmount:F2})" : $"↓ NERF (x{modAmount:F2})";
                _displayName = $"Stat Lottery: {statName} {dir}";
                NotificationService.Show($"{statName}: {dir}", null,
                    buff ? NotificationService.NotificationType.Reward : NotificationService.NotificationType.Unlucky);
                MegaChaos.Main.Msg($"[RandomStat] {statName} {dir} — permanent");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[RandomStat] " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd() { }
    }
}
