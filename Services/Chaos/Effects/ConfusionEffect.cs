using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Confusion — Slowly rolls the camera around the Z axis in a wobbly, unpredictable pattern.
    /// Different from Drunk (which sways position + FOV); this purely rotates the roll.
    /// </summary>
    public class ConfusionEffect : IChaosEffect
    {
        public string Id => "effect_confusion";
        public string Name => "Confusion";
        public string Description => "The camera slowly spins and rolls — which way is up?";
        public float DefaultDuration => 30f;

        private float _elapsed;
        private float _speed1;
        private float _speed2;
        private float _amp1;
        private float _amp2;

        public void OnStart()
        {
            _elapsed = 0f;
            _speed1 = Random.Range(0.4f, 0.9f);
            _speed2 = Random.Range(0.2f, 0.6f);
            _amp1   = Random.Range(20f, 45f);
            _amp2   = Random.Range(10f, 25f);
            NotificationService.Show("CONFUSION! Which way is up? 🌀", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt)
        {
            _elapsed += dt;
            // Combine two sin waves for an unpredictable roll
            float roll = Mathf.Sin(_elapsed * _speed1) * _amp1
                       + Mathf.Sin(_elapsed * _speed2 + 1.5f) * _amp2;

            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta
            {
                RollDeg = roll
            });
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Confusion cleared. Take a deep breath.", null, NotificationService.NotificationType.Reward);
        }
    }
}
