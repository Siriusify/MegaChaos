using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class SlowMotionEffect : IChaosEffect
    {
        public string Id => "effect_slowmotion";
        public string Name => "Bullet Time";
        public string Description => "Time enters slow motion — everything slows down!";
        public float DefaultDuration => 30f;

        private float _originalTimeScale;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = Random.Range(0.15f, 0.30f);
            NotificationService.Show("Time slowed down...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            Time.timeScale = _originalTimeScale;
            NotificationService.Show("Time back to normal!", null, NotificationService.NotificationType.Reward);
        }
    }
}
