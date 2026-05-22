using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class ColorTintEffect : IChaosEffect
    {
        public string Id => "effect_colortint";
        public string Name => "Color Filter";
        public string Description => "A strange color filter covers the screen!";
        public float DefaultDuration => 30f;

        private Color _color;

        public void OnStart()
        {
            float r = Random.Range(0f, 1f);
            float g = Random.Range(0f, 1f);
            float b = Random.Range(0f, 1f);
            _color = new Color(r, g, b, Random.Range(0.30f, 0.55f));

            NotificationService.Show("Color filter applied!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }

        public void OnGUI() 
        {
            if (_color.a > 0)
            {
                var oldColor = GUI.color;
                GUI.color = _color;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = oldColor;
            }
        }

        public void OnEnd()
        {
            NotificationService.Show("Color filter removed!", null, NotificationService.NotificationType.Reward);
        }
    }
}
