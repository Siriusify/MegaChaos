using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MegaChaos.Services;

namespace MegaChaos.Services.Chaos.Effects
{
    public class FloorIsLavaEffect : IChaosEffect
    {
        public string Id => "effect_floorislava";
        public string Name => "The Floor is Lava";
        public string Description => "The floor turns into deadly lava! Don't touch the ground.";
        public float DefaultDuration => 30f;

        private List<GameObject> _spawnedLava = new List<GameObject>();
        private float _damageTimer;
        private object _playerMovement;
        private object _playerHealth;

        public void OnStart()
        {
            _spawnedLava.Clear();
            _damageTimer = 0f;
            _playerMovement = null;
            _playerHealth = null;

            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player != null)
                {
                    _playerMovement = GameReflection.GetMember(player, "playerMovement");
                    var inventory = GameReflection.GetMember(player, "inventory");
                    _playerHealth = GameReflection.GetMember(inventory, "playerHealth");
                }

                // 1. Check for Lava prefab in memory
                var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
                GameObject lavaPrefab = null;

                if (allGos != null)
                {
                    foreach (var go in allGos)
                    {
                        if (go == null) continue;
                        
                        // Ignore the native challenge controller
                        if (go.name.Equals("TheFloorIsLava", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (lavaPrefab == null && go.name.IndexOf("Lava", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (go.GetComponent<Renderer>() != null)
                            {
                                lavaPrefab = go;
                            }
                        }
                    }
                }

                // 2. Fallback if Lava is not found
                bool isFakeLava = false;
                if (lavaPrefab == null)
                {
                    NotificationService.Show("Lava prefab not found in memory! Creating fake lava...", null, NotificationService.NotificationType.Warning);
                    lavaPrefab = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    lavaPrefab.name = "MegaChaos_FakeLava";
                    var renderer = lavaPrefab.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        renderer.material.color = new Color(1f, 0.4f, 0f, 0.8f); // Orange-red, slightly transparent
                        // Make it unlit if possible so it glows, but we can't easily change shader without assetbundle.
                    }
                    var col = lavaPrefab.GetComponent("Collider");
                    if (col != null) GameObject.Destroy(col);
                    isFakeLava = true;
                }

                // 3. Spawn Lava around the map
                if (player != null)
                {
                    var playerGo = GameReflection.GetMember(player, "gameObject") as GameObject;
                    Vector3 pPos = playerGo != null ? playerGo.transform.position : Vector3.zero;
                    
                    // Place it JUST above the player's current Y so it clips above the ground
                    float floorY = pPos.y + 0.15f; 

                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            var clone = GameObject.Instantiate(lavaPrefab);
                            clone.name = "MegaChaos_SpawnedLava";
                            clone.transform.position = new Vector3(pPos.x + (x * 30f), floorY, pPos.z + (z * 30f));
                            
                            if (isFakeLava)
                            {
                                // A Unity plane is 10x10. Scale 3 = 30x30.
                                clone.transform.localScale = new Vector3(3f, 1f, 3f);
                            }
                            else
                            {
                                // If it's the real lava, scaling it might distort it, but we have to make it big
                                clone.transform.localScale = new Vector3(5f, 1f, 5f);
                            }
                            
                            var col = clone.GetComponent("Collider");
                            if (col != null) GameObject.Destroy(col); // ensure it doesn't block walking

                            clone.SetActive(true);
                            _spawnedLava.Add(clone);
                        }
                    }
                }

                if (isFakeLava)
                {
                    lavaPrefab.SetActive(false);
                    GameObject.Destroy(lavaPrefab);
                }

                NotificationService.Show("THE FLOOR IS LAVA!", null, NotificationService.NotificationType.Warning);
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[FloorIsLava] OnStart: " + ex.Message);
            }
        }

        public void OnUpdate(float dt)
        {
            if (Time.timeScale <= 0.01f) return; // Oyuncu pause yaptıysa veya oyun durduysa hasar verme
            
            _damageTimer += dt;
            if (_damageTimer >= 0.5f) // Damage every 0.5s
            {
                _damageTimer = 0f;
                ApplyDamageIfOnFloor();
            }
        }

        private void ApplyDamageIfOnFloor()
        {
            if (_playerHealth == null || _playerMovement == null) return;

            try
            {
                bool isGrounded = false;
                
                // Try finding common grounded flags
                try { isGrounded = (bool)GameReflection.GetMember(_playerMovement, "isGrounded"); } catch { }
                if (!isGrounded) try { isGrounded = (bool)GameReflection.GetMember(_playerMovement, "Grounded"); } catch { }

                // Check velocity if flag not found or false
                if (!isGrounded)
                {
                    try
                    {
                        var velObj = GameReflection.GetMember(_playerMovement, "velocity");
                        if (velObj != null)
                        {
                            Vector3 vel = (Vector3)velObj;
                            // If not moving up or down significantly, likely on ground
                            if (Mathf.Abs(vel.y) < 0.1f) isGrounded = true;
                        }
                    }
                    catch { }
                }

                // If player is on the ground, apply lava damage!
                if (isGrounded)
                {
                    var method = _playerHealth.GetType().GetMethod("DamagePlayerExternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method != null)
                    {
                        // 10 damage per tick
                        method.Invoke(_playerHealth, new object[] { 10f, 0f, Vector3.zero, true, "Lava (MegaChaos)", 0, 0, null });
                    }
                }
            }
            catch { }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            foreach (var lava in _spawnedLava)
            {
                if (lava != null)
                {
                    if (lava.name.Equals("TheFloorIsLava", StringComparison.OrdinalIgnoreCase))
                    {
                        lava.SetActive(false);
                    }
                    else
                    {
                        GameObject.Destroy(lava);
                    }
                }
            }
            _spawnedLava.Clear();
            NotificationService.Show("The floor cooled down.", null, NotificationService.NotificationType.Reward);
        }
    }
}
