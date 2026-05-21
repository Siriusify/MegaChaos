using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class NoHudEffect : IChaosEffect
    {
        public string Id => "effect_nohud";
        public string Name => "No HUD";
        public string Description => "Can, XP, altın barları kaybolur — upgrade ekranları açık kalır!";
        public float DefaultDuration => 30f;

        private static readonly string[] HudTypeNames = { "InventoryHud", "XpAndGoldHUD" };
        private readonly List<(Behaviour comp, bool wasEnabled)> _saved = new();

        public void OnStart()
        {
            _saved.Clear();
            int count = 0;
            try
            {
                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in roots)
                {
                    count += TraverseAndDisable(root.transform);
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Warn($"[NoHud] Error: {ex.Message}");
            }

            if (count > 0)
                NotificationService.Show($"No HUD! ({count} components hidden)", null, NotificationService.NotificationType.Warning);
            else
                NotificationService.Show("No HUD: No HUD found in this scene.", null, NotificationService.NotificationType.Unlucky);
        }

        private int TraverseAndDisable(Transform t)
        {
            int found = 0;
            if (t == null) return found;

            try
            {
                var comps = t.GetComponents(typeof(UnityEngine.Behaviour));
                if (comps != null)
                {
                    foreach (var obj in comps)
                    {
                        var b = obj as Behaviour;
                        if (b == null) continue;

                        string typeName = b.GetType().Name;
                        bool match = false;
                        foreach (var target in HudTypeNames)
                        {
                            if (typeName == target) { match = true; break; }
                        }

                        if (match && b.enabled)
                        {
                            b.enabled = false;
                            _saved.Add((b, true));
                            found++;
                            MegaChaos.Main.Msg($"[NoHud] Disabled: {typeName} on '{b.gameObject.name}'");
                        }
                    }
                }
            }
            catch { }

            for (int i = 0; i < t.childCount; i++)
            {
                found += TraverseAndDisable(t.GetChild(i));
            }
            return found;
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            foreach (var (comp, wasEnabled) in _saved)
            {
                try { if (comp != null) comp.enabled = wasEnabled; } catch { }
            }
            _saved.Clear();
            NotificationService.Show("HUD restored!", null, NotificationService.NotificationType.Reward);
        }
    }
}
