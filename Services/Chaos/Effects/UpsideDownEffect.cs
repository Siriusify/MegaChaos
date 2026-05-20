using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class UpsideDownEffect : IChaosEffect
    {
        public string Id => "effect_upsidedown";
        public string Name => "Upside Down";
        public string Description => "Oyun dünyası tamamen ters yüz olur, kontrolleriniz birbirine girer!";
        public float DefaultDuration => 30f;
        
        private Camera _mainCamera;

        public void OnStart()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                var p = _mainCamera.projectionMatrix;
                p.m11 = -p.m11; // Y eksenini ters çevir
                p.m00 = -p.m00; // X eksenini ters çevir (Sol/Sağ da tersine dönsün)
                _mainCamera.projectionMatrix = p;
            }
            MegaChaos.Main.Msg("[MegaChaos] Dunya tersine dondu!");
        }
        
        public void OnUpdate(float deltaTime) { }
        
        public void OnGUI() { }
        
        public void OnEnd()
        {
            if (_mainCamera != null) 
            {
                _mainCamera.ResetProjectionMatrix();
            }
            MegaChaos.Main.Msg("[MegaChaos] Dunya normale dondu.");
        }
    }
}
