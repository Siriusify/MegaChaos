using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Lights Out — Draws a near-opaque dark overlay that pulses slightly, leaving only faint
    /// outlines visible. The player can barely see anything.
    /// </summary>
    public class LightsOutEffect : IChaosEffect
    {
        public string Id => "effect_lightsout";
        public string Name => "Lights Out";
        public string Description => "Darkness falls! The screen fades to near-black. Good luck.";
        public float DefaultDuration => 30f;

        private Texture2D _darkTex;
        private float _elapsed;

        public void OnStart()
        {
            _darkTex = new Texture2D(1, 1);
            _darkTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.92f));
            _darkTex.Apply();
            _elapsed = 0f;
            NotificationService.Show("LIGHTS OUT! Can you find your way?", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { _elapsed += dt; }

        public void OnGUI()
        {
            if (_darkTex == null) return;
            // Pulse opacity between 0.85 and 0.95 for a breathing darkness effect
            float alpha = 0.90f + Mathf.Sin(_elapsed * 1.2f) * 0.05f;
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _darkTex);
            GUI.color = old;
        }

        public void OnEnd()
        {
            if (_darkTex != null) { Object.Destroy(_darkTex); _darkTex = null; }
            NotificationService.Show("The lights are back on!", null, NotificationService.NotificationType.Reward);
        }
    }
}
