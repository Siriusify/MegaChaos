using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class TimeBendEffect : IChaosEffect
    {
        public string Id => "effect_time_bend";
        public string Name => "Time Warp";
        public string Description => "The flow of time in the game speeds up 3x!";
        public float DefaultDuration => 30f;

        private float _originalTimeScale;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 3f;
            MegaChaos.Main.Msg("[MegaChaos] Time accelerated!");
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            Time.timeScale = _originalTimeScale;
            MegaChaos.Main.Msg("[MegaChaos] Time back to normal.");
        }
    }
}
