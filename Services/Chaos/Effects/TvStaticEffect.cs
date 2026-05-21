using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// TV Static — Overlays a flickering noise texture to simulate a broken TV signal.
    /// Updates the noise every few frames for a performance-friendly static effect.
    /// </summary>
    public class TvStaticEffect : IChaosEffect
    {
        public string Id => "effect_tvstatic";
        public string Name => "TV Static";
        public string Description => "The screen fills with noise — like a broken TV signal.";
        public float DefaultDuration => 30f;

        private Texture2D _noiseTex;
        private float _updateTimer;
        private const float UpdateInterval = 0.06f; // refresh ~16fps to save perf
        private const int TexSize = 64;

        public void OnStart()
        {
            _noiseTex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            _noiseTex.filterMode = FilterMode.Point; // pixelated look
            GenerateNoise();
            NotificationService.Show("TV STATIC! 📺 Signal lost!", null, NotificationService.NotificationType.Warning);
        }

        private void GenerateNoise()
        {
            if (_noiseTex == null) return;
            var pixels = new Color32[TexSize * TexSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)Random.Range(0, 255);
                byte a = (byte)Random.Range(60, 140);
                pixels[i] = new Color32(v, v, v, a);
            }
            _noiseTex.SetPixels32(pixels);
            _noiseTex.Apply();
        }

        public void OnUpdate(float dt)
        {
            _updateTimer += dt;
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0f;
                GenerateNoise();
            }
        }

        public void OnGUI()
        {
            if (_noiseTex == null) return;
            var old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _noiseTex,
                ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        public void OnEnd()
        {
            if (_noiseTex != null) { Object.Destroy(_noiseTex); _noiseTex = null; }
            NotificationService.Show("Signal restored. Back to HD! 📺", null, NotificationService.NotificationType.Reward);
        }
    }
}
