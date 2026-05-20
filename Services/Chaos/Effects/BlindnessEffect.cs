using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class BlindnessEffect : IChaosEffect
    {
        public string Id => "effect_blindness";
        public string Name => "Körlük";
        public string Description => "Oyun dünyası kararır ama arayüz (HUD) görünmeye devam eder!";
        public float DefaultDuration => 15f;

        private int _originalCullingMask;
        private CameraClearFlags _originalClearFlags;
        private Color _originalBackgroundColor;
        private Camera _mainCamera;

        public void OnStart() 
        { 
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _originalCullingMask = _mainCamera.cullingMask;
                _originalClearFlags = _mainCamera.clearFlags;
                _originalBackgroundColor = _mainCamera.backgroundColor;

                // Sadece UI katmanı (Layer 5) renderlansın, gerisi gizlensin
                _mainCamera.cullingMask = 1 << 5; 
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
                _mainCamera.backgroundColor = Color.black;
            }
            MegaChaos.Main.Msg("[MegaChaos] Korluk basladi (HUD haric)!");
        }
        
        public void OnUpdate(float deltaTime) { }

        public void OnGUI() { }

        public void OnEnd() 
        { 
            if (_mainCamera != null)
            {
                _mainCamera.cullingMask = _originalCullingMask;
                _mainCamera.clearFlags = _originalClearFlags;
                _mainCamera.backgroundColor = _originalBackgroundColor;
            }
            MegaChaos.Main.Msg("[MegaChaos] Korluk bitti.");
        }
    }
}
