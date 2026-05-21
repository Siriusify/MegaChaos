using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Ekranın tamamına rastgele renkte yarı-saydam bir örtü çeker.
    /// Görüş bozulur ama tamamen kör olmaz.
    /// </summary>
    public class ColorTintEffect : IChaosEffect
    {
        public string Id => "effect_colortint";
        public string Name => "Color Filter";
        public string Description => "A strange color filter covers the screen!";
        public float DefaultDuration => 30f;

        private Texture2D _tintTex;

        public void OnStart()
        {
            _tintTex = new Texture2D(1, 1);
            float r = Random.Range(0f, 1f);
            float g = Random.Range(0f, 1f);
            float b = Random.Range(0f, 1f);
            _tintTex.SetPixel(0, 0, new Color(r, g, b, Random.Range(0.30f, 0.55f)));
            _tintTex.Apply();
            NotificationService.Show("Color filter applied!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }

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
            NotificationService.Show("Color filter removed!", null, NotificationService.NotificationType.Reward);
        }
    }
}
