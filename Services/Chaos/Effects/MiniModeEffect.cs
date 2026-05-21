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

        private float _shrinkScale = 1f;

        public void OnStart()
        {
            _playerTransform = FindPlayerTransform();
            if (_playerTransform != null)
            {
                _originalScale = _playerTransform.localScale;
                _shrinkScale = Random.Range(0.20f, 0.40f);
                _playerTransform.localScale = _originalScale * _shrinkScale;
                MegaChaos.Main.Msg($"[MiniMode] scale x{_shrinkScale:F2}");
            }
            NotificationService.Show("YOU SHRUNK! Everything looks huge!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) 
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player != null)
                {
                    var inventory = GameReflection.GetMember(player, "inventory");
                    if (inventory != null)
                    {
                        var playerStats = GameReflection.GetMember(inventory, "playerStats");
                        if (playerStats != null && _shrinkScale > 0)
                        {
                            GameReflection.SetMember(playerStats, "MoveSpeedMultiplier", 1f / _shrinkScale);
                        }
                    }
                }
            } catch { }
        }
        
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_playerTransform != null)
                _playerTransform.localScale = _originalScale;
            NotificationService.Show("Back to normal size, take a breath!", null, NotificationService.NotificationType.Reward);

            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player != null)
                {
                    var inventory = GameReflection.GetMember(player, "inventory");
                    if (inventory != null)
                    {
                        var playerStats = GameReflection.GetMember(inventory, "playerStats");
                        if (playerStats != null)
                        {
                            GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", System.Type.EmptyTypes);
                        }
                    }
                }
            } catch { }
        }
    }
}
