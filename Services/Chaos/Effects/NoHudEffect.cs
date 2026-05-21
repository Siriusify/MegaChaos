using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class NoHudEffect : IChaosEffect
    {
        public string Id => "effect_nohud";
        public string Name => "No HUD";
        public string Description => "All UI elements disappear. Good luck!";
        public float DefaultDuration => 30f;

        private static readonly string[] TargetPaths = {
            "GameUI/GameUI/HUD/Minimap/Border",
            "GameUI/GameUI/HUD/Minimap/MapRenderer",
            "GameUI/PauseUI/Main/Inventory",
            "GameUI/PauseUI/Main/W_Stats",
            "GameUI/GameUI/HUD",
            "GameUI/GameUI/EncounterWindows/InventoryOverlay/W_Inventory",
            "GameUI/GameUI/EncounterWindows/InventoryOverlay/W_Stats (1)",
            "AlwaysManagers/AlwaysUI/Canvas/Debug"
        };

        private readonly List<GameObject> _hiddenObjects = new();

        public void OnStart()
        {
            _hiddenObjects.Clear();
            int count = 0;

            foreach (var path in TargetPaths)
            {
                var obj = FindByPath(path);
                if (obj != null)
                {
                    obj.SetActive(false);
                    _hiddenObjects.Add(obj);
                    count++;
                }
            }

            if (count > 0)
                NotificationService.Show($"No HUD! ({count} elements hidden)", null, NotificationService.NotificationType.Warning);
            else
                NotificationService.Show("No HUD elements found to hide.", null, NotificationService.NotificationType.Unlucky);
        }

        private GameObject FindByPath(string path)
        {
            try
            {
                string[] parts = path.Split('/');
                if (parts.Length == 0) return null;
                
                GameObject current = GameObject.Find(parts[0]);
                if (current == null) return null;
                
                for (int i = 1; i < parts.Length; i++)
                {
                    Transform child = current.transform.Find(parts[i]);
                    if (child == null) return null;
                    current = child.gameObject;
                }
                return current;
            }
            catch { return null; }
        }

        public void OnUpdate(float dt) 
        { 
            // Force disable them continuously in case the game tries to re-enable them (e.g. opening pause menu)
            foreach (var obj in _hiddenObjects)
            {
                if (obj != null && obj.activeSelf)
                {
                    obj.SetActive(false);
                }
            }
        }
        
        public void OnGUI() { }

        public void OnEnd()
        {
            foreach (var obj in _hiddenObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            _hiddenObjects.Clear();
            NotificationService.Show("HUD restored!", null, NotificationService.NotificationType.Reward);
        }
    }
}
