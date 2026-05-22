using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Sanki internet bağlantısı kesiliyormuş gibi rastgele anlık donmalar (micro-freeze) yapar.
    /// </summary>
    public class PacketLossEffect : IChaosEffect, IChaosPauseAwareEffect
    {
        public string Id => "effect_packetloss";
        public string Name => "Packet Loss";
        public string Description => "The game randomly freezes, as if you're losing connection!";
        public float DefaultDuration => 30f;

        private float _nextFreezeIn;
        private float _freezeRemaining;
        private float _originalTimeScale;
        private bool _frozen;
        private GUIStyle _style;

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            _frozen = false;
            _freezeRemaining = 0f;
            _ScheduleNextFreeze();
            NotificationService.Show("Connection issues! Ping: 999ms", null, NotificationService.NotificationType.Warning);
        }

        private void _ScheduleNextFreeze()
        {
            _nextFreezeIn = Random.Range(0.3f, 1.6f);
        }

        public void OnUpdate(float dt)
        {
            if (!_frozen)
            {
                _nextFreezeIn -= dt;
                if (_nextFreezeIn <= 0f)
                {
                    _frozen = true;
                    _originalTimeScale = Time.timeScale > 0 ? Time.timeScale : 1f;
                    Time.timeScale = 0f;
                    _freezeRemaining = Random.Range(0.12f, 0.45f);
                }
            }
        }

        public void OnPauseState(bool isTimePaused, bool isMenuOpen)
        {
            // Eğer biz dondurduysak ve oyunun kendi menüsü açık değilse dondurma süremizi ilerlet
            if (_frozen && !isMenuOpen)
            {
                _freezeRemaining -= Time.unscaledDeltaTime;
                if (_freezeRemaining <= 0f)
                {
                    Time.timeScale = _originalTimeScale;
                    _frozen = false;
                    _ScheduleNextFreeze();
                }
            }
        }

        public void OnGUI()
        {
            if (!_frozen) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 18,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
            }
            GUI.Label(new Rect(10, 10, 300, 30), "⚠ CONNECTION LOST...", _style);
        }

        public void OnEnd()
        {
            if (_frozen)
            {
                Time.timeScale = _originalTimeScale;
                _frozen = false;
            }
            NotificationService.Show("Connection restored! Ping: 12ms", null, NotificationService.NotificationType.Reward);
        }
    }
}
