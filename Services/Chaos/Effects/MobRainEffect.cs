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
        public string Name => "Mob Rain";
        public string Description => "A large number of enemies suddenly spawn around you!";
        public float DefaultDuration => 0f; // anlık

        public void OnStart()
        {
            int spawned = 0;
            int wantedCount = UnityEngine.Random.Range(8, 15);
            GameObject spawnFxTemplate = null;
            try { spawnFxTemplate = GameObject.Find("EnemySpawnFx(Clone)"); } catch { }

            try
            {
                var enemyType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Enemies.Enemy", "Assets.Scripts.Actors.Enemies.Enemy", "Enemy");
                if (enemyType != null)
                {
                    var allEnemies = GameReflection.FindObjectsOfTypeAll(enemyType);
                    var aliveTemplates = new List<UnityEngine.Object>();

                    if (allEnemies != null)
                    {
                        foreach (var obj in allEnemies)
                        {
                            var enemyObj = obj as UnityEngine.Object;
                            if (enemyObj == null) continue;
                            
                            // Check if it's a prefab or an active valid enemy
                            var go = GameReflection.GetMember(enemyObj, "gameObject") as GameObject;
                            if (go != null && !go.name.Contains("Boss")) 
                            {
                                aliveTemplates.Add(enemyObj);
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
                                
                                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                                float dist  = UnityEngine.Random.Range(7f, 15f);
                                var spawnPos = playerPos + new Vector3(Mathf.Cos(angle) * dist, 0.5f, Mathf.Sin(angle) * dist);

                                var clone = UnityEngine.Object.Instantiate(template, spawnPos, Quaternion.identity);
                                if (clone != null)
                                {
                                    spawned++;
                                    if (spawnFxTemplate != null)
                                    {
                                        try
                                        {
                                            UnityEngine.Object.Instantiate(spawnFxTemplate, spawnPos, Quaternion.identity);
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MegaChaos.Main.Warn("[MobRain] Klonlama hatası: " + ex.Message);
                            }
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
