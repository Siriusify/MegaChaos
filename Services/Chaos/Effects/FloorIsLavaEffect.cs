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

                // 1. Check for Lava prefab in memory using safe Hierarchy navigation (Avoids IL2CPP Stripping errors)
                GameObject lavaPrefab = null;

                try
                {
                    // First try to find CryptGeneration which holds the Lava
                    var cryptGen = GameObject.Find("CryptGeneration");
                    if (cryptGen != null)
                    {
                        lavaPrefab = FindLavaRecursive(cryptGen.transform);
                    }

                    // Fallback to direct find
                    if (lavaPrefab == null)
                    {
                        var directLava = GameObject.Find("Lava");
                        if (directLava != null && directLava.GetComponent("Renderer") != null)
                        {
                            lavaPrefab = directLava;
                        }
                    }
                }
                catch (Exception e)
                {
                    MegaChaos.Main.Warn("[FloorIsLava] Find lava error: " + e.Message);
                }

                // 2. Fallback if Lava is not found
                bool isFakeLava = false;
                if (lavaPrefab == null)
                {
                    NotificationService.Show("Lava prefab not found in memory! Creating fake lava...", null, NotificationService.NotificationType.Warning);
                    lavaPrefab = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    lavaPrefab.name = "MegaChaos_FakeLava";
                    var renderer = lavaPrefab.GetComponent("Renderer");
                    if (renderer != null)
                    {
                        var mat = GameReflection.GetMember(renderer, "material");
                        if (mat != null) GameReflection.SetMember(mat, "color", new Color(1f, 0.4f, 0f, 0.8f));
                    }
                    
                    // Add native Lava component via reflection
                    try
                    {
                        var lavaType = GameReflection.FindType("Lava", "", "Assembly-CSharp");
                        if (lavaType != null)
                        {
                            var addComp = typeof(GameObject).GetMethod("AddComponent", new[] { typeof(Type) });
                            if (addComp != null)
                            {
                                var lavaComp = addComp.Invoke(lavaPrefab, new object[] { lavaType });
                                if (lavaComp != null)
                                {
                                    // Configure it
                                    GameReflection.SetMember(lavaComp, "damage", 10f);
                                    GameReflection.SetMember(lavaComp, "damageInterval", 0.5f);
                                    GameReflection.SetMember(lavaComp, "isDamageZone", true);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MegaChaos.Main.Warn("[FloorIsLava] Could not add native Lava component: " + ex.Message);
                    }
                    var col = lavaPrefab.GetComponent("Collider");
                    if (col != null) GameReflection.SetMember(col, "isTrigger", true); // Don't block walking, but allow Lava script to detect touch
                    isFakeLava = true;
                }

                // 3. Spawn ONE massive Lava to cover the map
                if (player != null)
                {
                    var playerGo = GameReflection.GetMember(player, "gameObject") as GameObject;
                    Vector3 pPos = playerGo != null ? playerGo.transform.position : Vector3.zero;
                    
                    // Place it JUST above the player's current Y so it clips above the ground
                    float floorY = pPos.y + 0.15f; 

                    var clone = GameObject.Instantiate(lavaPrefab);
                    clone.name = "MegaChaos_SpawnedLava";
                    clone.transform.position = new Vector3(pPos.x, floorY, pPos.z);
                    
                    if (isFakeLava)
                    {
                        clone.transform.localScale = new Vector3(30f, 1f, 30f); // 300x300 meters
                    }
                    else
                    {
                        // Real lava might distort, but we need it big
                        clone.transform.localScale = new Vector3(50f, 1f, 50f);
                    }
                    
                    var cloneCol = clone.GetComponent("Collider");
                    if (cloneCol != null) GameReflection.SetMember(cloneCol, "isTrigger", true);

                    // UPDATE THE LAVA SCRIPT BOUNDS!
                    var lavaComp = clone.GetComponent("Lava");
                    if (lavaComp != null)
                    {
                        // Create a massive damage zone around the player
                        Bounds newBounds = new Bounds(new Vector3(pPos.x, floorY, pPos.z), new Vector3(1000f, 20f, 1000f));
                        GameReflection.SetMember(lavaComp, "bounds", newBounds);
                        
                        // Force damage and interval again just to be safe
                        GameReflection.SetMember(lavaComp, "damage", 10f);
                        GameReflection.SetMember(lavaComp, "damageInterval", 0.5f);
                        GameReflection.SetMember(lavaComp, "isDamageZone", true);
                    }

                    clone.SetActive(true);
                    _spawnedLava.Add(clone);
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

        public void OnGUI() 
        { 
            // 100% reliable visual indicator: Tint the screen orange
            var oldColor = GUI.color;
            // Pulsing orange color
            float alpha = 0.2f + Mathf.PingPong(Time.time * 0.5f, 0.2f);
            GUI.color = new Color(1f, 0.3f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private GameObject FindLavaRecursive(Transform parent)
        {
            if (parent == null) return null;
            
            if (parent.name.IndexOf("Lava", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (parent.GetComponent("Renderer") != null)
                {
                    return parent.gameObject;
                }
            }
            
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindLavaRecursive(parent.GetChild(i));
                if (result != null) return result;
            }
            
            return null;
        }

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
