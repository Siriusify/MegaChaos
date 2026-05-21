using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class BlindnessEffect : IChaosEffect
    {
        public string Id => "effect_blindness";
        public string Name => "Blindness";
        public string Description => "The game world turns completely black but the UI stays visible!";
        public float DefaultDuration => 30f;

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
            MegaChaos.Main.Msg("[MegaChaos] Blindness started.");
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
            MegaChaos.Main.Msg("[MegaChaos] Blindness ended.");
        }
    }
}
