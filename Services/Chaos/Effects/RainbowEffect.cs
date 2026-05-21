using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Rainbow World — Cycles the camera background through the full hue spectrum,
    /// tinting the screen with a rolling prismatic color overlay.
    /// </summary>
    public class RainbowEffect : IChaosEffect
    {
        public string Id => "effect_rainbow";
        public string Name => "Rainbow World";
        public string Description => "The screen constantly changes colors!";
        public float DefaultDuration => 30f;

        private Texture2D _tintTex;
        private float _elapsed;
        private float _speed;

        public void OnStart()
        {
            _tintTex = new Texture2D(1, 1);
            _elapsed = 0f;
            _speed = Random.Range(0.5f, 1.5f);
            NotificationService.Show("RAINBOW WORLD! 🌈 Enjoy the colors!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt)
        {
            _elapsed += dt;
            if (_tintTex == null) return;
            // Full hue cycle — convert HSV to RGB
            float hue = (_elapsed * _speed * 0.2f) % 1f;
            Color c = Color.HSVToRGB(hue, 0.9f, 1f);
            c.a = 0.28f;
            _tintTex.SetPixel(0, 0, c);
            _tintTex.Apply();
        }

        public void OnGUI()
        {
            if (_tintTex == null) return;
            var old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _tintTex);
            GUI.color = old;
        }

        public void OnEnd()
        {
            if (_tintTex != null) { Object.Destroy(_tintTex); _tintTex = null; }
            NotificationService.Show("Back to boring normal colors.", null, NotificationService.NotificationType.Reward);
        }
    }
}
