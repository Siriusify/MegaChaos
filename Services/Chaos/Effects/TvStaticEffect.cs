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
        public string Description => "The screen is covered in old TV static!";
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
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float v = Random.Range(0f, 1f);
                    float a = Random.Range(0.25f, 0.55f);
                    _noiseTex.SetPixel(x, y, new Color(v, v, v, a));
                }
            }
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
            float sw = Screen.width > 100 ? Screen.width : 1920f;
            float sh = Screen.height > 100 ? Screen.height : 1080f;

            var old = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _noiseTex, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        public void OnEnd()
        {
            if (_noiseTex != null) { Object.Destroy(_noiseTex); _noiseTex = null; }
            NotificationService.Show("Signal restored. Back to HD! 📺", null, NotificationService.NotificationType.Reward);
        }
    }
}
