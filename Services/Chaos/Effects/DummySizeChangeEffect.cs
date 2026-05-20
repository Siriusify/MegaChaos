using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class DummySizeChangeEffect : IChaosEffect
    {
        public string Id => "effect_size_change";
        public string Name => "Büyüme İksiri";
        public string Description => "Karakteriniz 30 saniyeliğine dev gibi olur!";
        
        public float DefaultDuration => 30f;

        private Vector3 _originalScale;
        private Transform _playerTransform;

        private Transform FindPlayerTransform()
        {
            var gameManagerType = GameReflection.FindType("Il2CppAssets.Scripts.Managers.GameManager", "Assets.Scripts.Managers.GameManager", "GameManager");
            if (gameManagerType == null) return null;
            
            var gameManagerInstance = GameReflection.InvokeStatic(gameManagerType, "get_Instance", System.Type.EmptyTypes);
            if (gameManagerInstance == null) return null;
            
            var player = GameReflection.GetMember(gameManagerInstance, "player");
            if (player == null) return null;

            var transformObj = GameReflection.GetMember(player, "transform");
            return transformObj as Transform;
        }

        public void OnStart()
        {
            _playerTransform = FindPlayerTransform();
            
            if (_playerTransform != null)
            {
                _originalScale = _playerTransform.localScale;
                _playerTransform.localScale = _originalScale * 3f; 
                MegaChaos.Main.Msg("[MegaChaos] Oyuncu boyutu 3 katına çıkarıldı!");
            }
            else
            {
                MegaChaos.Main.Msg("[MegaChaos] Uyarı: Büyüme İksiri için oyuncu objesi bulunamadı.");
            }
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_playerTransform != null)
            {
                _playerTransform.localScale = _originalScale;
                MegaChaos.Main.Msg("[MegaChaos] Oyuncu boyutu normale döndü.");
            }
        }
    }
}
