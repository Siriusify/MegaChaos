using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Oyunu süre boyunca aşırı hızlandırır (hipermod).
    /// TimeBendEffect'ten farklı olarak MUCH faster — kafayı yiyen hız.
    /// </summary>
    public class HyperSpeedEffect : IChaosEffect
    {
        public string Id => "effect_hyperspeed";
        public string Name => "Hyper Speed";
        public string Description => "Oyun delicesine hızlanır — her şey fırtına gibi geçer!";
        public float DefaultDuration => 30f;

        private float _originalTimeScale;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = Random.Range(3f, 6f);
            NotificationService.Show("HYPER SPEED ACTIVATED!", null, NotificationService.NotificationType.Warning);
            MegaChaos.Main.Msg($"[HyperSpeed] timeScale={Time.timeScale}");
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            Time.timeScale = _originalTimeScale;
            NotificationService.Show("Speed back to normal.", null, NotificationService.NotificationType.Reward);
        }
    }
}
