using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class DummySizeChangeEffect : IChaosEffect
    {
        public string Id => "effect_size_change";
        public string Name => "Growth Potion";
        public string Description => "Your character becomes a giant!";
        
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
                MegaChaos.Main.Msg("[MegaChaos] Player size tripled!");
            }
            else
            {
                MegaChaos.Main.Msg("[MegaChaos] Warning: Player object not found for Growth Potion.");
            }
        }

        public void OnUpdate(float deltaTime) 
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
                        if (playerStats != null)
                        {
                            // If size tripled, speed should be 1/3 (0.33f)
                            GameReflection.SetMember(playerStats, "MoveSpeedMultiplier", 0.33f);
                        }
                    }
                }
            } catch { }
        }
        
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_playerTransform != null)
            {
                _playerTransform.localScale = _originalScale;
                MegaChaos.Main.Msg("[MegaChaos] Player size restored.");
            }
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
