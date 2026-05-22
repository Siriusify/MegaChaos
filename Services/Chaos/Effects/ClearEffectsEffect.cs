using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class ClearEffectsEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_cleareffects";
        public string Name => "Chaos Cleanser";
        public string Description => "Clears all currently active timed chaos effects!";
        public float DefaultDuration => 0f;

        public bool HideProgressBar => true;

        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        public void OnStart()
        {
            ChaosEngine.Instance.ClearActiveTimedEffects();
            NotificationService.Show("CLEANSER: All active timed effects cleared!", null, NotificationService.NotificationType.Reward);
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
