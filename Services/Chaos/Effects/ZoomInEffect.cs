using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class ZoomInEffect : IChaosEffect
    {
        public string Id => "effect_zoomin";
        public string Name => "Yakın Plan";
        public string Description => "Kamera aniden çok yaklaşır, etraf görmek zorlaşır!";
        public float DefaultDuration => 8f;

        private float _fovDelta; // negatif = yakınlaştır

        public void OnStart()
        {
            // Hedef FOV rastgele 15-25, ama base ne olursa olsun delta kayıt et
            float targetFov = Random.Range(15f, 25f);
            // Stack'teki base FOV = Camera.main.fieldOfView (diğer deltalar dahil değil, ham base)
            // Yakın plan: çok düşük FOV → büyük negatif delta
            // Gerçek base'i bilmiyoruz ama 60 varsayalım; stack zaten toplar
            _fovDelta = targetFov - 60f; // 60 oyunun baz FOV'u (stack base ile uyumluk için sabit)
            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta { FovOffset = _fovDelta });
            NotificationService.Show("YAKLAŞ! Görmek zorlaşıyor!", null, NotificationService.NotificationType.Unlucky);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Zoom normale döndü!", null, NotificationService.NotificationType.Reward);
        }
    }
}
