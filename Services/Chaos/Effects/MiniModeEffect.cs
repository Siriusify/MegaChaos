using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class MiniModeEffect : IChaosEffect
    {
        public string Id => "effect_minimode";
        public string Name => "Shrink Potion";
        public string Description => "Your character shrinks! Everything looks huge.";
        public float DefaultDuration => 30f;

        private Vector3 _originalScale;
        private Transform _playerTransform;
        private float _shrinkScale = 1f;
        private object _statInv;
        private object _speedModifier;
        private bool _scaleApplied;

        private Transform FindPlayerTransform()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player == null) return null;

                var transformObj = GameReflection.GetMember(player, "transform") as Transform;
                if (transformObj != null) return transformObj;

                var go = GameReflection.GetMember(player, "gameObject") as GameObject;
                return go != null ? go.transform : null;
            }
            catch { return null; }
        }

        public void OnStart()
        {
            _playerTransform = FindPlayerTransform();
            
            if (_playerTransform != null)
            {
                _shrinkScale = UnityEngine.Random.Range(0.20f, 0.40f);
                _originalScale = _playerTransform.localScale;
                _playerTransform.localScale = _originalScale * _shrinkScale;
                MegaChaos.Main.Msg($"[MiniMode] scale x{_shrinkScale:F2}");
                ApplySpeedModifier(1f / _shrinkScale);
                _scaleApplied = true;
            }
            NotificationService.Show("YOU SHRUNK! Everything looks huge!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime) 
        {
            if (_scaleApplied && _playerTransform != null)
            {
                _playerTransform.localScale = _originalScale * _shrinkScale;
            }
        }
        
        public void OnGUI() { }

        public void OnEnd()
        {
            _scaleApplied = false;
            if (_playerTransform != null)
            {
                _playerTransform.localScale = _originalScale;
            }
            TryRemoveSpeedModifier();
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

        private void ApplySpeedModifier(float multiplier)
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory = GameReflection.GetMember(player, "inventory");
                _statInv = GameReflection.GetMember(inventory, "statInventory");
                var playerStats = GameReflection.GetMember(inventory, "playerStats");

                var eStatType = GameReflection.FindType("Il2CppAssets.Scripts.Menu.Shop.EStat", "Assets.Scripts.Menu.Shop.EStat", "EStat");
                var eStatModifyType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.EStatModifyType", "EStatModifyType");
                var statModifierType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.StatModifier",
                    "Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "StatModifier");

                if (_statInv == null || eStatType == null || eStatModifyType == null || statModifierType == null)
                    return;

                object chosenStat = Enum.Parse(eStatType, "MoveSpeedMultiplier");
                object modType = Enum.Parse(eStatModifyType, "Multiplication");

                _speedModifier = Activator.CreateInstance(statModifierType);
                GameReflection.SetMember(_speedModifier, "stat", chosenStat);
                GameReflection.SetMember(_speedModifier, "modifyType", modType);
                GameReflection.SetMember(_speedModifier, "modification", multiplier);

                GameReflection.InvokeInstance(_statInv, "ChangeStat",
                    new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                    _speedModifier, true, 0f, false);

                if (playerStats != null)
                    GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
            }
            catch { }
        }

        private void TryRemoveSpeedModifier()
        {
            if (_statInv == null || _speedModifier == null) return;
            try
            {
                bool removed = false;
                var modifierType = _speedModifier.GetType();
                var removeMethod = GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveStat")
                    ?? GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveStatModifier")
                    ?? GameReflection.FindAnyMethod(_statInv.GetType(), "RemoveModifier");

                if (removeMethod != null)
                {
                    try
                    {
                        removeMethod.Invoke(_statInv, new[] { _speedModifier });
                        removed = true;
                    }
                    catch { }
                }

                if (!removed)
                {
                    var statModifierType = GameReflection.FindType(
                        "Il2CppAssets.Scripts.Inventory__Items__Pickups.Stats.StatModifier",
                        "Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier", "StatModifier");
                    var invMod = Activator.CreateInstance(statModifierType);
                    GameReflection.SetMember(invMod, "stat", GameReflection.GetMember(_speedModifier, "stat"));
                    GameReflection.SetMember(invMod, "modifyType", GameReflection.GetMember(_speedModifier, "modifyType"));
                    GameReflection.SetMember(invMod, "modification", _shrinkScale);

                    GameReflection.InvokeInstance(_statInv, "ChangeStat",
                        new[] { statModifierType, typeof(bool), typeof(float), typeof(bool) },
                        invMod, true, 0f, false);
                }
            }
            catch { }
        }
    }
}
