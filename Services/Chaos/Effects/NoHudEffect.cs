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
                // 1. Search in all loaded scenes
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    var roots = scene.GetRootGameObjects();
                    foreach (var root in roots)
                    {
                        var transforms = root.GetComponentsInChildren<Transform>(true);
                        foreach (var t in transforms)
                        {
                            if (t != null && GetFullPath(t).EndsWith(path, StringComparison.OrdinalIgnoreCase))
                            {
                                return t.gameObject;
                            }
                        }
                    }
                }

                // 2. Search in DontDestroyOnLoad by finding a known active object there
                var alwaysMgr = GameObject.Find("AlwaysManagers");
                if (alwaysMgr != null)
                {
                    var transforms = alwaysMgr.transform.root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in transforms)
                    {
                        if (t != null && GetFullPath(t).EndsWith(path, StringComparison.OrdinalIgnoreCase))
                        {
                            return t.gameObject;
                        }
                    }
                }

                return null;
            }
            catch { return null; }
        }

        private string GetFullPath(Transform current)
        {
            if (current == null) return "";
            if (current.parent == null) return current.name;
            return GetFullPath(current.parent) + "/" + current.name;
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
