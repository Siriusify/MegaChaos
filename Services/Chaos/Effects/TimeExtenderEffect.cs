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
            int multiplier = UnityEngine.Random.Range(2, 11);
            ChaosEngine.Instance.ExtendAllActive(multiplier, multiplier);
            NotificationService.Show($"TIME EXTENDER: Active effects duration x{multiplier}!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
