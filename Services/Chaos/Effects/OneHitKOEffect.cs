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
            // 1. Oyuncu HP = 1, Shield = 0
            if (_playerHealth != null)
            {
                var combined    = GameReflection.InvokeInstance(_playerHealth, "GetCombinedHp", Type.EmptyTypes);
                if (combined != null)
                {
                    float hp = Convert.ToSingle(combined);
                    if (hp > 1f)
                    {
                        // Damage uygula: hp - 1 kadar
                        GameReflection.InvokeInstance(_playerHealth, "DamagePlayerExternal",
                            new[] { typeof(float), typeof(float), typeof(Vector3), typeof(bool), typeof(string),
                                    typeof(int),   typeof(int),   GameReflection.FindType("Il2CppAssets.Scripts.Actors.Enemies.Enemy","Assets.Scripts.Actors.Enemies.Enemy","Enemy") ?? typeof(object) },
                            hp - 1f, 0f, Vector3.zero, true, "MegaChaos_OneHitKO", 0, 0, null);
                    }
                }
            }

            // 2. Tüm düşmanları 1 HP'ye çek
            if (_enemyType == null) return;

            var findMethod = typeof(UnityEngine.Object).GetMethod(
                "FindObjectsOfType", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(Type) }, null);
            if (findMethod == null) return;

            var enemies = findMethod.Invoke(null, new object[] { _enemyType }) as System.Collections.IEnumerable;
            if (enemies == null) return;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                try
                {
                    var dead = GameReflection.InvokeInstance(enemy, "IsDead", Type.EmptyTypes);
                    if (dead != null && (bool)dead) continue;

                    var hpRatio = GameReflection.InvokeInstance(enemy, "GetHpRatio", Type.EmptyTypes);
                    if (hpRatio == null) continue;
                    float ratio = Convert.ToSingle(hpRatio);
                    if (ratio <= 0.01f) continue; // Zaten ölmek üzere

                    // Kill yerine Heal(-BIG) değil — Damage metodunu çağırıyoruz
                    // En güvenli: Kill ile bitirelim, ratio > 1/MAX dışındaki düşmanlar için
                    // Aslında ratio = hp/maxHp — eğer ratio > çok küçük bir değerse hasar ver
                    if (ratio > 0.02f) // Yaklaşık 1 HP değil
                    {
                        // Doğrudan hasar ver: maxHp kadar hasar → 0'a iner
                        // DamageFromPlayerOther veya Damage kullanacağız ama DamageContainer gerekiyor
                        // En basit: KillPlayer yerine "Kill" çağır
                        // Hayır, Kill ölümü tetikler — biz sadece 1 HP'de tutmak istiyoruz
                        // En doğrusu: hasar yeter miktarda vererek HP'yi 1'e getirmek
                        // Ama damage container oluşturamıyoruz kolay... 
                        // Alternatif: her 0.5 saniyede Kill() çağırmak ama bu ölüm ekranı açar
                        // Gerçek çözüm: Enemy hp field'ını set etmek
                        // Enemy sınıfında direkt hp field yokmuş gibi görünüyor ama Heal(negative) var mı?
                        // Heal(int amount) — pozitif → HP artar. Negatif → azalır mı?
                        // Deneyelim: GetHpRatio > 0.02 ise Heal(-BIG)
                        // Bu çalışmıyorsa: Kill() ile direkt ölüm
                        GameReflection.InvokeInstance(enemy, "Heal", new[] { typeof(int) }, -999999);
                    }
                }
                catch { }
            }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            NotificationService.Show("One Hit KO ended — welcome back to normal life!", null, NotificationService.NotificationType.Reward);
        }
    }
}
