using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// DVD Ekran Koruyucu:
    /// - Tüm ekran simsiyah örtü (4 siyah panel)
    /// - Sadece DVD logosu penceresi boş bırakılır — oyun oradan görünür
    /// - Logo sektiğinde renk değişir
    /// - TAMAMEN pure GUI: Hiçbir texture yükleme yok → sıfır çökme riski
    ///   (ImageConversion.LoadImage IL2CPP GPU context dışında crash yapıyordu)
    /// </summary>
    public class DvdScreensaverEffect : IChaosEffect
    {
        public string Id => "effect_dvd";
        public string Name => "DVD Screensaver";
        public string Description => "Ekranın büyük kısmı simsiyah! Sadece DVD penceresinden görebilirsin.";
        public float DefaultDuration => 30f;

        private float _x, _y;
        private float _vx, _vy;
        private Color _color;

        // Pencere boyutu
        private const float W = 280f;
        private const float H = 120f;

        // Çizim için gerekli tek texture (1×1 pixel, renk = beyaz, tinting ile boyar)
        private Texture2D _whiteTex;
        private GUIStyle _labelStyle;
        private bool _stylesInit;

        public void OnStart()
        {
            float sw = Screen.width  > 100 ? Screen.width  : 1920f;
            float sh = Screen.height > 100 ? Screen.height : 1080f;

            _x = UnityEngine.Random.Range(0f, sw - W);
            _y = UnityEngine.Random.Range(0f, sh - H);

            float speed = UnityEngine.Random.Range(120f, 220f);
            float angle = UnityEngine.Random.Range(25f, 65f) * Mathf.Deg2Rad;
            _vx = Mathf.Cos(angle) * speed * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
            _vy = Mathf.Sin(angle) * speed * (UnityEngine.Random.value > 0.5f ? 1f : -1f);

            _color = NextColor();

            // Tek texture: 1×1 beyaz — GUI.color tinting ile her rengi alır
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();

            _stylesInit = false;

            NotificationService.Show("📀 DVD Screensaver started!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt)
        {
            float sw = Screen.width  > 100 ? Screen.width  : 1920f;
            float sh = Screen.height > 100 ? Screen.height : 1080f;

            _x += _vx * dt;
            _y += _vy * dt;

            bool bounced = false;

            if (_x < 0f)        { _x = 0f;       _vx =  Mathf.Abs(_vx); bounced = true; }
            if (_x + W > sw)    { _x = sw - W;   _vx = -Mathf.Abs(_vx); bounced = true; }
            if (_y < 0f)        { _y = 0f;       _vy =  Mathf.Abs(_vy); bounced = true; }
            if (_y + H > sh)    { _y = sh - H;   _vy = -Mathf.Abs(_vy); bounced = true; }

            if (bounced) _color = NextColor();
        }

        public void OnGUI()
        {
            if (_whiteTex == null) return;
            if (!_stylesInit) InitStyles();

            float sw = Screen.width  > 100 ? Screen.width  : 1920f;
            float sh = Screen.height > 100 ? Screen.height : 1080f;

            var oldColor = GUI.color;

            // ── Siyah örtü: 4 panel (logo alanı boş bırakılır) ─────────────
            GUI.color = Color.black;

            // Üst
            if (_y > 0)
                GUI.DrawTexture(new Rect(0, 0, sw, _y), _whiteTex);

            // Alt
            float bottomY = _y + H;
            if (bottomY < sh)
                GUI.DrawTexture(new Rect(0, bottomY, sw, sh - bottomY), _whiteTex);

            // Sol (logo yüksekliği aralığında)
            if (_x > 0)
                GUI.DrawTexture(new Rect(0, _y, _x, H), _whiteTex);

            // Sağ
            float rightX = _x + W;
            if (rightX < sw)
                GUI.DrawTexture(new Rect(rightX, _y, sw - rightX, H), _whiteTex);

            // ── Renkli çerçeve ──────────────────────────────────────────────
            GUI.color = _color;
            float b = 4f; // border thickness
            // Üst çizgi
            GUI.DrawTexture(new Rect(_x, _y, W, b), _whiteTex);
            // Alt çizgi
            GUI.DrawTexture(new Rect(_x, _y + H - b, W, b), _whiteTex);
            // Sol çizgi
            GUI.DrawTexture(new Rect(_x, _y, b, H), _whiteTex);
            // Sağ çizgi
            GUI.DrawTexture(new Rect(_x + W - b, _y, b, H), _whiteTex);

            // ── Yarı saydam koyu arka plan (pencere iç dolgusu) ─────────────
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(_x + b, _y + b, W - 2*b, H - 2*b), _whiteTex);

            // ── DVD Metin / Logo ─────────────────────────────────────────────
            GUI.color = _color;
            // Alt başlık satırı
            GUI.Label(new Rect(_x, _y + H * 0.15f, W, H * 0.55f), "DVD", _labelStyle);
            // Küçük "Video" alt yazısı
            GUI.Label(new Rect(_x, _y + H * 0.62f, W, H * 0.35f), "▶ VIDEO", _subStyle);

            GUI.color = oldColor;
        }

        private GUIStyle _subStyle;

        private void InitStyles()
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _labelStyle.normal.textColor = Color.white;

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _subStyle.normal.textColor = Color.white;

            _stylesInit = true;
        }

        private static readonly Color[] Palette =
        {
            new Color(0.00f, 0.80f, 1.00f),   // Cyan
            new Color(1.00f, 0.25f, 0.75f),   // Hot Pink
            new Color(0.20f, 1.00f, 0.20f),   // Lime
            new Color(1.00f, 0.80f, 0.00f),   // Gold
            new Color(1.00f, 0.40f, 0.10f),   // Orange
            new Color(0.60f, 0.20f, 1.00f),   // Violet
            new Color(1.00f, 1.00f, 1.00f),   // White
            new Color(0.20f, 0.90f, 0.70f),   // Teal
        };

        private static Color NextColor()
            => Palette[UnityEngine.Random.Range(0, Palette.Length)];

        public void OnEnd()
        {
            if (_whiteTex != null)
            {
                UnityEngine.Object.Destroy(_whiteTex);
                _whiteTex = null;
            }
            NotificationService.Show("DVD Screensaver ended!", null, NotificationService.NotificationType.Reward);
        }
    }
}
