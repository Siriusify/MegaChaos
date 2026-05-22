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

        private float _enemyTick;
        private object _statInv;
        private object _damageModifier;
        private object _enemyDamageModifier;
        private bool _boostApplied;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var statInv      = GameReflection.GetMember(inventory, "statInventory");
                
                if (statInv == null) { NotificationService.Show("One Hit KO failed.", null, NotificationService.NotificationType.Unlucky); return; }

                _enemyTick = 0f;
                _boostApplied = false;
                ApplyDamageBoost(statInv);
                ForcePlayerOneHp(player, inventory);
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
                    ForcePlayerOneHp(player, inventory);
                    if (!_boostApplied)
                    {
                        var statInv = GameReflection.GetMember(inventory, "statInventory");
                        ApplyDamageBoost(statInv);
                    }

                    _enemyTick += dt;
                    if (_enemyTick >= 0.5f)
                    {
                        _enemyTick = 0f;
                        ForceEnemiesOneHp();
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
            TryRemoveModifier(_damageModifier);
            TryRemoveModifier(_enemyDamageModifier);
        }

        private void ForcePlayerOneHp(object player, object inventory)
        {
            if (inventory == null) return;
            var playerStats = GameReflection.GetMember(inventory, "playerStats");
            var playerHealth = GameReflection.GetMember(inventory, "playerHealth");

            if (playerStats != null)
            {
                GameReflection.SetMember(playerStats, "MaxHealth", 1f);
                GameReflection.SetMember(playerStats, "DamageMultiplier", 9999f);
                GameReflection.SetMember(playerStats, "EnemyDamageMultiplier", 9999f);
                GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
            }

            if (playerHealth != null)
            {
                var currentHealth = GameReflection.GetMember(playerHealth, "currentHealth");
                if (currentHealth is float h && h > 1f)
                    GameReflection.SetMember(playerHealth, "currentHealth", 1f);
            }
        }

        private void ForceEnemiesOneHp()
        {
            try
            {
                var enemyType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Actors.Enemies.Enemy",
                    "Assets.Scripts.Actors.Enemies.Enemy",
                    "Enemy");
                if (enemyType == null) return;

                var enemies = GameReflection.FindObjectsOfType(enemyType);
                if (enemies == null) return;

                foreach (var obj in enemies)
                {
                    if (obj == null) continue;

                    object health = GameReflection.GetMember(obj, "enemyHealth")
                                   ?? GameReflection.GetMember(obj, "health")
                                   ?? obj;

                    GameReflection.SetMember(health, "currentHealth", 1f);
                    GameReflection.SetMember(health, "maxHealth", 1f);
                }
            }
            catch { }
        }

        private void ApplyDamageBoost(object statInv)
        {
            if (statInv == null) return;
            try
            {
                var eStatType = GameReflection.FindType("Il2CppAssets.Scripts.Menu.Shop.EStat", "Assets.Scripts.Menu.Shop.EStat", "EStat");
                var eStatModifyType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType", "EStatModifyType");
                var statModifierType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.StatModifier",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "StatModifier");

                if (eStatType == null || eStatModifyType == null || statModifierType == null)
                    return;

                _statInv = statInv;
                object modType = Enum.Parse(eStatModifyType, "Multiplication");

                _damageModifier = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(_damageModifier, "stat", Enum.Parse(eStatType, "DamageMultiplier"));
                GameReflection.SetMember(_damageModifier, "modifyType", modType);
                GameReflection.SetMember(_damageModifier, "modification", 9999f);

                _enemyDamageModifier = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(_enemyDamageModifier, "stat", Enum.Parse(eStatType, "EnemyDamageMultiplier"));
                GameReflection.SetMember(_enemyDamageModifier, "modifyType", modType);
                GameReflection.SetMember(_enemyDamageModifier, "modification", 9999f);

                GameReflection.InvokeInstance(_statInv, "ChangeStat",
                    new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                    _damageModifier, false, DefaultDuration, false);

                GameReflection.InvokeInstance(_statInv, "ChangeStat",
                    new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                    _enemyDamageModifier, false, DefaultDuration, false);

                _boostApplied = true;
            }
            catch { }
        }

        private void TryRemoveModifier(object modifier)
        {
            if (_statInv == null || modifier == null) return;
            try
            {
                var modifierType = modifier.GetType();
                var removeMethod = GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveStat")
                    ?? GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveStatModifier")
                    ?? GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveModifier");

                if (removeMethod != null && removeMethod.GetParameters().Length == 1
                    && removeMethod.GetParameters()[0].ParameterType == modifierType)
                {
                    removeMethod.Invoke(_statInv, new[] { modifier });
                }
            }
            catch { }
        }
    }
}
