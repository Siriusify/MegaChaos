using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class TimeExtenderEffect : IChaosEffect
    {
        public string Id => "effect_timeextender";
        public string Name => "Time Extender";
        public string Description => "Multiplies the remaining time of all active chaos effects by 2x to 10x!";
        public float DefaultDuration => 0f;

        public void OnStart()
        {
            float multiplier = UnityEngine.Random.Range(1.5f, 3.5f);
            
            // Multiply future effects
            var profile = ProfileManager.ActiveProfile;
            if (profile != null)
            {
                profile.ChaosDurationMultiplier *= multiplier;
            }

            // Multiply active effects
            ChaosEngine.Instance.ExtendAllActive(multiplier, multiplier);
            
            NotificationService.Show($"TIME EXTENDER: All effect durations multiplied by x{multiplier:F1}!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
