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
                var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                if (behaviours != null)
                {
                    foreach (var b in behaviours)
                    {
                        if (b == null) continue;
                        string typeName = b.GetType().Name;
                        bool match = false;
                        foreach (var target in HudTypeNames) {
                            if (typeName == target) { match = true; break; }
                        }
                        if (match && b.enabled)
                        {
                            b.enabled = false;
                            _saved.Add((b, true));
                            count++;
                            MegaChaos.Main.Msg($"[NoHud] Disabled: {typeName} on '{b.gameObject.name}'");
                        }
                    }
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
