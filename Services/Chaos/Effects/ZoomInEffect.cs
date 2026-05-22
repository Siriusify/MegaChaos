using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class ZoomInEffect : IChaosEffect
    {
        public string Id => "effect_zoomin";
        public string Name => "Zoom In";
        public string Description => "The camera suddenly zooms in, making it hard to see around you!";
        public float DefaultDuration => 30f;

        private float _fovDelta; // negatif = yakınlaştır

        public void OnStart()
        {
            // Hedef FOV rastgele 15-25, ama base ne olursa olsun delta kayıt et
            float targetFov = Random.Range(15f, 25f);
            float baseFov = 60f;
            var cam = Camera.main;
            if (cam != null)
                baseFov = cam.fieldOfView;

            _fovDelta = targetFov - baseFov;
            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta { FovOffset = _fovDelta });
            NotificationService.Show("ZOOM IN! Vision narrowing!", null, NotificationService.NotificationType.Unlucky);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Zoom back to normal!", null, NotificationService.NotificationType.Reward);
        }
    }
}
