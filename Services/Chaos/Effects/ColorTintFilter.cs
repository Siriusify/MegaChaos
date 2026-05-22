using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    internal sealed class ColorTintFilter : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private Material _material;
        private Color _color;

        public void SetColor(Color color)
        {
            _color = color;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_material == null)
            {
                var shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Graphics.Blit(src, dest);
                    return;
                }
                _material = new Material(shader);
            }

            _material.SetColor(ColorId, _color);
            Graphics.Blit(src, dest, _material);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}
