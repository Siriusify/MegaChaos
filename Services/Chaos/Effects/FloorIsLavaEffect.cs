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

                // 1. Check for native "TheFloorIsLava" object first
                var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
                GameObject theFloorIsLavaNative = null;
                GameObject lavaPrefab = null;

                if (allGos != null)
                {
                    foreach (var go in allGos)
                    {
                        if (go == null) continue;
                        
                        if (go.name.Equals("TheFloorIsLava", StringComparison.OrdinalIgnoreCase))
                        {
                            theFloorIsLavaNative = go;
                        }
                        else if (lavaPrefab == null && go.name.IndexOf("Lava", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (go.GetComponent<Renderer>() != null || go.GetComponent("Collider") != null)
                            {
                                lavaPrefab = go;
                            }
                        }
                    }
                }

                if (theFloorIsLavaNative != null)
                {
                    NotificationService.Show("Native Lava Floor activated!", null, NotificationService.NotificationType.Warning);
                    theFloorIsLavaNative.SetActive(true);
                    _spawnedLava.Add(theFloorIsLavaNative); // Store it to disable later
                }
                else
                {
                    // 2. Fallback if Lava is not found
                    if (lavaPrefab == null)
                    {
                        NotificationService.Show("Lava prefab not found in memory! Creating fake lava...", null, NotificationService.NotificationType.Warning);
                        lavaPrefab = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        lavaPrefab.name = "MegaChaos_FakeLava";
                        var renderer = lavaPrefab.GetComponent<Renderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.color = new Color(1f, 0.4f, 0f, 0.9f); // Orange-red
                        }
                        var col = lavaPrefab.GetComponent("Collider");
                        if (col != null) GameObject.Destroy(col);
                    }

                    // 3. Spawn Lava around the map
                    if (player != null)
                    {
                        var playerGo = GameReflection.GetMember(player, "gameObject") as GameObject;
                        Vector3 pPos = playerGo != null ? playerGo.transform.position : Vector3.zero;
                        float floorY = pPos.y - 0.5f; // Guess the floor height

                        // Create a massive lava floor
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int z = -1; z <= 1; z++)
                            {
                                var clone = GameObject.Instantiate(lavaPrefab);
                                clone.name = "MegaChaos_SpawnedLava";
                                clone.transform.position = new Vector3(pPos.x + (x * 50f), floorY, pPos.z + (z * 50f));
                                
                                // Scale it huge so it covers the map
                                clone.transform.localScale = new Vector3(20f, 1f, 20f);
                                
                                var col = clone.GetComponent("Collider");
                                if (col != null) GameObject.Destroy(col); // ensure it doesn't block walking

                                clone.SetActive(true);
                                _spawnedLava.Add(clone);
                            }
                        }
                    }

                    if (lavaPrefab.name == "MegaChaos_FakeLava")
                    {
                        lavaPrefab.SetActive(false);
                        GameObject.Destroy(lavaPrefab);
                    }
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
