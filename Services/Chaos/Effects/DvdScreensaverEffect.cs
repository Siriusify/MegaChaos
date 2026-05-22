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
        public string Description => "The screen goes black! You can only see through the bouncing DVD window.";
        public float DefaultDuration => 30f;

        private float _x, _y;
        private float _vx, _vy;
        private Color _color;

        // Pencere boyutu
        private const float W = 280f;
        private const float H = 120f;

        private Texture2D _logoTex;
        private Texture2D _whiteTex;
        private GUIStyle _labelStyle;
        private GUIStyle _subStyle;
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

            // White fill texture for drawing overlays
            _whiteTex = new Texture2D(1, 1);
            _whiteTex.SetPixel(0, 0, Color.white);
            _whiteTex.Apply();
            _whiteTex.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;

            _logoTex = BuildLogoTexture();
            if (_logoTex != null)
                _logoTex.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.HideAndDontSave;

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

            // ── DVD Logo / Metin ─────────────────────────────────────────────
            if (_logoTex != null)
            {
                // Draw the actual PNG logo with color tint
                GUI.color = _color;
                float padding = b + 4f;
                GUI.DrawTexture(
                    new Rect(_x + padding, _y + padding, W - padding * 2f, H - padding * 2f),
                    _logoTex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                if (!_stylesInit) InitStyles();
                GUI.color = _color;
                GUI.Label(new Rect(_x, _y + H * 0.15f, W, H * 0.55f), "DVD", _labelStyle);
                GUI.Label(new Rect(_x, _y + H * 0.62f, W, H * 0.35f), "▶ VIDEO", _subStyle);
            }

            GUI.color = oldColor;
        }

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

        private Texture2D BuildLogoTexture()
        {
            try
            {
                const int texW = 160;
                const int texH = 64;
                var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);

                // Clear to transparent
                for (int y = 0; y < texH; y++)
                for (int x = 0; x < texW; x++)
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));

                string[] d =
                {
                    "11110",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "11110"
                };
                string[] v =
                {
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "01010",
                    "00100"
                };

                int scale = 6;
                int glyphW = 5 * scale;
                int glyphH = 7 * scale;
                int gap = 6;
                int totalW = glyphW * 3 + gap * 2;
                int startX = (texW - totalW) / 2;
                int startY = (texH - glyphH) / 2;

                DrawGlyph(tex, d, startX, startY, scale);
                DrawGlyph(tex, v, startX + glyphW + gap, startY, scale);
                DrawGlyph(tex, d, startX + (glyphW + gap) * 2, startY, scale);

                tex.Apply();
                return tex;
            }
            catch
            {
                return null;
            }
        }

        private void DrawGlyph(Texture2D tex, string[] glyph, int startX, int startY, int scale)
        {
            for (int y = 0; y < glyph.Length; y++)
            {
                var row = glyph[y];
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] != '1') continue;
                    for (int yy = 0; yy < scale; yy++)
                    for (int xx = 0; xx < scale; xx++)
                    {
                        int px = startX + x * scale + xx;
                        int py = startY + (glyph.Length - 1 - y) * scale + yy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, Color.white);
                    }
                }
            }
        }

        public void OnEnd()
        {
            if (_whiteTex != null)
            {
                UnityEngine.Object.Destroy(_whiteTex);
                _whiteTex = null;
            }
            if (_logoTex != null)
            {
                UnityEngine.Object.Destroy(_logoTex);
                _logoTex = null;
            }
            NotificationService.Show("DVD Screensaver ended!", null, NotificationService.NotificationType.Reward);
        }
    }
}
