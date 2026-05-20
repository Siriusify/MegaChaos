using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Actors.Enemies;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// MobRain — Sahnedeki mevcut düşmanları yeni pozisyonlarda klonlayarak spawn eder.
    /// IL2CPP generic metotlar kullanarak doğrudan Enemy sınıfına erişir ve 100% kararlılıkla klonlama yapar.
    /// </summary>
    public class MobRainEffect : IChaosEffect
    {
        public string Id => "effect_mobrain";
        public string Name => "Mob Yağmuru";
        public string Description => "Etrafa aniden çok sayıda düşman yağar!";
        public float DefaultDuration => 0f; // anlık

        public void OnStart()
        {
            int spawned = 0;
            int wantedCount = UnityEngine.Random.Range(8, 15);

            try
            {
                // Doğrudan generic Object.FindObjectsOfType<Enemy>() çağrısı yapıyoruz.
                // Bu yöntem IL2CPP altında reflection'a kıyasla 100% güvenilirdir.
                var activeEnemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
                var aliveTemplates = new List<Enemy>();

                if (activeEnemies != null)
                {
                    foreach (var enemy in activeEnemies)
                    {
                        if (enemy == null) continue;
                        try
                        {
                            // Canlı olan düşmanları şablon olarak topla
                            var dead = GameReflection.InvokeInstance(enemy, "IsDead", Type.EmptyTypes);
                            if (dead != null && (bool)dead) continue;
                            aliveTemplates.Add(enemy);
                        }
                        catch
                        {
                            // Fallback: IsDead kontrolü patlarsa yine de ekle
                            aliveTemplates.Add(enemy);
                        }
                    }
                }

                if (aliveTemplates.Count > 0)
                {
                    var playerPos = GetPlayerPosition();
                    for (int i = 0; i < wantedCount; i++)
                    {
                        try
                        {
                            var template = aliveTemplates[UnityEngine.Random.Range(0, aliveTemplates.Count)];
                            
                            // Oyuncunun etrafında dairesel rastgele pozisyon
                            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                            float dist  = UnityEngine.Random.Range(7f, 15f);
                            var spawnPos = playerPos + new Vector3(Mathf.Cos(angle) * dist, 0.5f, Mathf.Sin(angle) * dist);

                            var clone = UnityEngine.Object.Instantiate(template, spawnPos, Quaternion.identity);
                            if (clone != null)
                            {
                                spawned++;
                            }
                        }
                        catch (Exception ex)
                        {
                            MegaChaos.Main.Warn("[MobRain] Klonlama hatası: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[MobRain] Hata: " + ex.Message + "\n" + ex.StackTrace);
            }

            if (spawned > 0)
            {
                NotificationService.Show($"MOB YAĞMURU! {spawned} yeni düşman indi! 👾", null, NotificationService.NotificationType.Warning);
                MegaChaos.Main.Msg($"[MobRain] Success: spawned={spawned} enemies.");
            }
            else
            {
                NotificationService.Show("Mob Yağmuru: Şu an sahnede klonlanacak aktif düşman yok.", null, NotificationService.NotificationType.Unlucky);
                MegaChaos.Main.Warn("[MobRain] Failed to spawn: No active enemies to clone.");
            }
        }

        private Vector3 GetPlayerPosition()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var t = GameReflection.GetMember(player, "transform") as Transform;
                return t?.position ?? Vector3.zero;
            }
            catch { return Vector3.zero; }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
