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
        public string Description => "Herkes 1 HP'de kalır — ilk vuruş öldürür!";
        public float DefaultDuration => 30f;

        private object _playerHealth;
        private object _playerInventory;
        private Type _enemyType;
        private Type _damageContainerType;
        private float _refreshTimer;
        private const float RefreshInterval = 0.5f;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                _playerInventory = GameReflection.GetMember(player, "inventory");
                _playerHealth    = GameReflection.GetMember(_playerInventory, "playerHealth");

                _enemyType         = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Enemies.Enemy", "Assets.Scripts.Actors.Enemies.Enemy", "Enemy");
                _damageContainerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.DamageContainer", "Assets.Scripts.Actors.DamageContainer", "DamageContainer");

                _refreshTimer = 0f;

                // Hemen uygula
                ApplyOneHitKO();

                NotificationService.Show("ONE HIT KO! You have 1 HP. Good luck 😈", null, NotificationService.NotificationType.Warning);
            }
            catch (Exception ex) { MegaChaos.Main.Error("[OneHitKO] OnStart: " + ex.Message); }
        }

        public void OnUpdate(float dt)
        {
            _refreshTimer += dt;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                try { ApplyOneHitKO(); }
                catch { }
            }
        }

        private void ApplyOneHitKO()
        {
            // 1. Oyuncu HP = 1
            if (_playerHealth != null)
            {
                var combined = GameReflection.InvokeInstance(_playerHealth, "GetCombinedHp", Type.EmptyTypes);
                if (combined != null)
                {
                    float hp = Convert.ToSingle(combined);
                    if (hp > 1f)
                    {
                        var method = _playerHealth.GetType().GetMethod("DamagePlayerExternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(_playerHealth, new object[] { hp - 1f, 0f, Vector3.zero, true, "MegaChaos_OneHitKO", 0, 0, null });
                        }
                    }
                }
            }

            // 2. Tüm düşmanları 1 HP'ye çek (veya canlarını çok azalt)
            if (_enemyType == null) return;
            try
            {
                var enemies = GameReflection.FindObjectsOfType(_enemyType);
                if (enemies == null) return;

                foreach (var obj in enemies)
                {
                    if (obj == null) continue;

                    var dead = GameReflection.InvokeInstance(obj, "IsDead", Type.EmptyTypes);
                    if (dead != null && (bool)dead) continue;

                    // Düşmanların canını 1 yapmak için scale çarpanını sıfıra yaklaştır
                    GameReflection.InvokeInstance(obj, "SetSwarmMultiplierHp", new[] { typeof(float) }, 0.00001f);
                    
                    // Alternatif olarak, eğer field isimlerini biliyorsak doğrudan canı 1'e ayarlayabiliriz.
                    try { GameReflection.SetMember(obj, "hp", 1f); } catch { }
                    try { GameReflection.SetMember(obj, "_hp", 1f); } catch { }
                    try { GameReflection.SetMember(obj, "currentHp", 1f); } catch { }
                }
            }
            catch { }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            NotificationService.Show("One Hit KO ended — welcome back to normal life!", null, NotificationService.NotificationType.Reward);
        }
    }
}
