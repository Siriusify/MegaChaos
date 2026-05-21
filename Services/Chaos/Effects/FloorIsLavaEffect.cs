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

        private float _elapsed;
        private float _startY;
        private float _targetY;

        public void OnStart()
        {
            _spawnedLava.Clear();
            _damageTimer = 0f;
            _elapsed = 0f;
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
                catch { }

                if (player != null)
                {
                    var playerGo = GameReflection.GetMember(player, "gameObject") as GameObject;
                    Vector3 pPos = playerGo != null ? playerGo.transform.position : Vector3.zero;
                    
                    // Start below the floor, target is center of bounds (approx 12 units high)
                    _startY = pPos.y - 6f;
                    _targetY = pPos.y + 12f;
                    
                    if (officialLava != null)
                    {
                        officialLava.transform.position = new Vector3(pPos.x, _startY, pPos.z);
                        officialLava.SetActive(true);
                        _spawnedLava.Add(officialLava);
                    }
                    else
                    {
                        var fakeLava = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        fakeLava.name = "MegaChaos_FakeLava";
                        fakeLava.transform.position = new Vector3(pPos.x, _startY, pPos.z);
                        fakeLava.transform.localScale = new Vector3(30f, 1f, 30f);
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
            if (Time.timeScale <= 0.01f) return;
            
            _elapsed += dt;

            // Animate Lava Rising and Lowering
            float currentY = _startY;
            float totalDuration = 30f; // Assuming default 30s
            float riseTime = totalDuration - 5f; // Rise for 25s
            
            if (_elapsed < riseTime)
            {
                float t = _elapsed / riseTime;
                currentY = Mathf.Lerp(_startY, _targetY, t);
            }
            else
            {
                float t = (_elapsed - riseTime) / 5f; // Lower in last 5s
                currentY = Mathf.Lerp(_targetY, _startY, t);
            }

            foreach (var lava in _spawnedLava)
            {
                if (lava != null)
                {
                    lava.transform.position = new Vector3(lava.transform.position.x, currentY, lava.transform.position.z);
                }
            }

            _damageTimer += dt;
            if (_damageTimer >= 0.5f)
            {
                _damageTimer = 0f;
                ApplyDamageIfOnFloor(currentY);
            }
        }

        private void ApplyDamageIfOnFloor(float lavaY)
        {
            if (_playerHealth == null || _playerMovement == null) return;

            try
            {
                var playerGo = GameReflection.GetMember(_playerMovement, "gameObject") as GameObject;
                if (playerGo == null) return;

                // Player takes damage if their Y position is at or below the lava level
                float pY = playerGo.transform.position.y;
                if (pY <= lavaY + 0.8f) // 0.8f offset so they get hit if standing slightly inside it
                {
                    var method = _playerHealth.GetType().GetMethod("DamagePlayerExternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(_playerHealth, new object[] { 10f, 0f, Vector3.zero, true, "Lava (MegaChaos)", 0, 0, null });
                    }
                }
            }
            catch { }
        }

        public void OnGUI() 
        { 
            // Removed orange pulsing overlay as requested
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
