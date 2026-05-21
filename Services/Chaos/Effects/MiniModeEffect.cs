using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Oyuncuyu geçici olarak mini yapar (0.2x-0.4x scale).
    /// </summary>
    public class MiniModeEffect : IChaosEffect
    {
        public string Id => "effect_minimode";
        public string Name => "Shrink Potion";
        public string Description => "Your character shrinks! Everything looks huge.";
        public float DefaultDuration => 30f;

        private Vector3 _originalScale;
        private Transform _playerTransform;

        private Transform FindPlayerTransform()
        {
            var gameManagerType = GameReflection.FindType("Il2CppAssets.Scripts.Managers.GameManager", "Assets.Scripts.Managers.GameManager", "GameManager");
            if (gameManagerType == null) return null;
            var gm     = GameReflection.InvokeStatic(gameManagerType, "get_Instance", System.Type.EmptyTypes);
            var player = GameReflection.GetMember(gm, "player");
            return GameReflection.GetMember(player, "transform") as Transform;
        }

        public void OnStart()
        {
            _playerTransform = FindPlayerTransform();
            if (_playerTransform != null)
            {
                _originalScale = _playerTransform.localScale;
                float s = Random.Range(0.20f, 0.40f);
                _playerTransform.localScale = _originalScale * s;
                MegaChaos.Main.Msg($"[MiniMode] scale x{s:F2}");
            }
            NotificationService.Show("YOU SHRUNK! Everything looks huge!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_playerTransform != null)
                _playerTransform.localScale = _originalScale;
            NotificationService.Show("Back to normal size, take a breath!", null, NotificationService.NotificationType.Reward);
        }
    }
}
