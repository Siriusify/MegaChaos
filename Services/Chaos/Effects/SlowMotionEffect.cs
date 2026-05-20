using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class SlowMotionEffect : IChaosEffect
    {
        public string Id => "effect_slowmotion";
        public string Name => "Bullet Time";
        public string Description => "Zaman ağır çekime geçer — her şey yavaşlar!";
        public float DefaultDuration => 6f;

        private float _originalTimeScale;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = Random.Range(0.15f, 0.30f);
            NotificationService.Show("Zaman yavaşladı...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            Time.timeScale = _originalTimeScale;
            NotificationService.Show("Zaman normale döndü!", null, NotificationService.NotificationType.Reward);
        }
    }
}
