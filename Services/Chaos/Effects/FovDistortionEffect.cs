using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class FovDistortionEffect : IChaosEffect
    {
        public string Id => "effect_fov";
        public string Name => "Mide Bulantısı";
        public string Description => "Kamera görüş açısı (FOV) sürekli genişleyip daralarak baş döndürür!";
        public float DefaultDuration => 20f;

        private float _timer;

        public void OnStart()
        {
            _timer = 0f;
            NotificationService.Show("Baş dönüyor...", null, NotificationService.NotificationType.Warning);
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
            NotificationService.Show("Mide bulantısı geçti.", null, NotificationService.NotificationType.Reward);
        }
    }
}
