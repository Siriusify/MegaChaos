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

        private GameObject _quad;
        private Color _color;

        public void OnStart()
        {
            float r = Random.Range(0f, 1f);
            float g = Random.Range(0f, 1f);
            float b = Random.Range(0f, 1f);
            _color = new Color(r, g, b, Random.Range(0.30f, 0.55f));

            var cam = Camera.main;
            if (cam != null)
            {
                _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _quad.layer = 2; // Ignore Raycast
                _quad.transform.SetParent(cam.transform, false);
                _quad.transform.localPosition = new Vector3(0, 0, cam.nearClipPlane + 0.1f);
                _quad.transform.localRotation = Quaternion.identity;

                var shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = _color;
                    _quad.GetComponent<Renderer>().material = mat;
                }
            }

            NotificationService.Show("Color filter applied!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) 
        {
            if (_quad != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    _quad.transform.SetParent(cam.transform, false); // in case cam changed
                    float z = cam.nearClipPlane + 0.1f;
                    _quad.transform.localPosition = new Vector3(0, 0, z);
                    _quad.transform.localRotation = Quaternion.identity;
                    
                    float h = 2.0f * z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    float w = h * cam.aspect;
                    _quad.transform.localScale = new Vector3(w, h, 1f);
                }
            }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            if (_quad != null) { Object.Destroy(_quad); _quad = null; }
            NotificationService.Show("Color filter removed!", null, NotificationService.NotificationType.Reward);
        }
    }
}
