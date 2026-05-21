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

                // 1. Try to find the game's official TheFloorIsLava object!
                GameObject officialLava = null;
                try
                {
                    var worldEdge = GameObject.Find("WorldEdgeTop");
                    if (worldEdge != null)
                    {
                        var lavaTrans = worldEdge.transform.Find("TheFloorIsLava");
                        if (lavaTrans != null)
                        {
                            officialLava = lavaTrans.gameObject;
                        }
                    }
                }
                catch (Exception e)
                {
                    MegaChaos.Main.Warn("[FloorIsLava] Error finding WorldEdgeTop/TheFloorIsLava: " + e.Message);
                }

                if (player != null)
                {
                    var playerGo = GameReflection.GetMember(player, "gameObject") as GameObject;
                    Vector3 pPos = playerGo != null ? playerGo.transform.position : Vector3.zero;
                    float floorY = pPos.y + 0.2f; // Just slightly above the player's feet
                    
                    if (officialLava != null)
                    {
                        // Use the game's official lava!
                        officialLava.transform.position = new Vector3(pPos.x, floorY, pPos.z);
                        officialLava.SetActive(true);
                        _spawnedLava.Add(officialLava); // We will disable it on end
                    }
                    else
                    {
                        // 2. Fallback: Create Fake Lava
                        NotificationService.Show("Official lava not found! Creating fake lava...", null, NotificationService.NotificationType.Warning);
                        var fakeLava = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        fakeLava.name = "MegaChaos_FakeLava";
                        fakeLava.transform.position = new Vector3(pPos.x, floorY, pPos.z);
                        fakeLava.transform.localScale = new Vector3(30f, 1f, 30f); // 300x300 meters
                        fakeLava.SetActive(true);
                        _spawnedLava.Add(fakeLava);
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
