using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class TimeBendEffect : IChaosEffect
    {
        public string Id => "effect_time_bend";
        public string Name => "Time Warp";
        public string Description => "Oyunun zaman akışı 3 kat hızlanır!";
        public float DefaultDuration => 30f;

        private float _originalTimeScale;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 3f;
            MegaChaos.Main.Msg("[MegaChaos] Zaman hizlandirildi!");
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            Time.timeScale = _originalTimeScale;
            MegaChaos.Main.Msg("[MegaChaos] Zaman normale dondu.");
        }
    }
}
