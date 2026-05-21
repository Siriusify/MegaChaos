using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class ScreenShakeEffect : IChaosEffect
    {
        public string Id => "effect_screenshake";
        public string Name => "Earthquake";
        public string Description => "The camera shakes violently, making the ground feel like it's slipping!";
        public float DefaultDuration => 30f;

        private float _elapsed;
        private float _intensity;
        private float _freqX;
        private float _freqY;

        public void OnStart()
        {
            _elapsed   = 0f;
            _intensity = Random.Range(0.10f, 0.50f);
            _freqX     = Random.Range(15f, 35f);
            _freqY     = Random.Range(10f, 25f);
            NotificationService.Show("EARTHQUAKE! The ground is shaking!", null, NotificationService.NotificationType.Warning);
            MegaChaos.Main.Msg($"[Deprem] intensity={_intensity:F2} freqX={_freqX:F1} freqY={_freqY:F1}");
        }

        public void OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;
            float x = Mathf.Sin(_elapsed * _freqX) * _intensity;
            float y = Mathf.Sin(_elapsed * _freqY) * _intensity * 0.6f;

            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta
            {
                PosOffset = new Vector3(x, y, 0f)
            });
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Earthquake stopped, you can breathe now!", null, NotificationService.NotificationType.Reward);
        }
    }
}
