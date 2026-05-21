using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Vignette — Draws pulsing dark elliptical edges that close in over time,
    /// creating a claustrophobic tunnel-vision effect.
    /// </summary>
    public class VignetteEffect : IChaosEffect
    {
        public string Id => "effect_vignette";
        public string Name => "Tunnel Vision";
        public string Description => "The edges of the screen close in — claustrophobia incoming!";
        public float DefaultDuration => 30f;

        private Texture2D _black;
        private float _elapsed;
        private float _pulseSpeed;

        public void OnStart()
        {
            _black = new Texture2D(1, 1);
            _black.SetPixel(0, 0, Color.black);
            _black.Apply();
            _elapsed = 0f;
            _pulseSpeed = Random.Range(0.5f, 1.2f);
            NotificationService.Show("TUNNEL VISION! The world is closing in...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { _elapsed += dt; }

        public void OnGUI()
        {
            if (_black == null) return;

            // Pulsing border thickness: base 20% of screen, +/- 5%
            float pct = 0.18f + Mathf.Sin(_elapsed * _pulseSpeed) * 0.06f;
            int w = Screen.width;
            int h = Screen.height;
            int bx = (int)(w * pct);
            int by = (int)(h * pct);

            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.95f);

            // Four border rects
            GUI.DrawTexture(new Rect(0, 0, bx, h), _black);            // Left
            GUI.DrawTexture(new Rect(w - bx, 0, bx, h), _black);       // Right
            GUI.DrawTexture(new Rect(0, 0, w, by), _black);            // Top
            GUI.DrawTexture(new Rect(0, h - by, w, by), _black);       // Bottom

            GUI.color = old;
        }

        public void OnEnd()
        {
            if (_black != null) { Object.Destroy(_black); _black = null; }
            NotificationService.Show("Vision restored. You can breathe again.", null, NotificationService.NotificationType.Reward);
        }
    }
}
