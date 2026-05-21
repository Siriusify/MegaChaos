using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class FovDistortionEffect : IChaosEffect
    {
        public string Id => "effect_fov";
        public string Name => "Nausea";
        public string Description => "The camera FOV constantly expands and shrinks, making you dizzy!";
        public float DefaultDuration => 30f;

        private float _timer;

        public void OnStart()
        {
            _timer = 0f;
            NotificationService.Show("Head spinning...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime)
        {
            _timer += deltaTime;
            float fovDelta = Mathf.Sin(_timer * 3f) * 30f;

            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta
            {
                FovOffset = fovDelta
            });
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Nausea gone.", null, NotificationService.NotificationType.Reward);
        }
    }
}
