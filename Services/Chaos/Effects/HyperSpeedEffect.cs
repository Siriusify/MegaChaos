using System;
using System.Collections.Generic;
using MegaChaos.Services;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Oyunu süre boyunca aşırı hızlandırır (hipermod).
    /// TimeBendEffect'ten farklı olarak MUCH faster — kafayı yiyen hız.
    /// </summary>
    public class HyperSpeedEffect : IChaosEffect, IChaosPauseAwareEffect
    {
        public string Id => "effect_hyperspeed";
        public string Name => "Hyper Speed";
        public string Description => "The game goes crazy fast — everything moves like a storm!";
        public float DefaultDuration => 30f;

        private float _originalTimeScale;
        private float _originalFixedDeltaTime;
        private float _fastTimeScale;
        private bool _suppressed;
        private readonly Dictionary<string, float> _managerOriginals = new();

        private static readonly string[] ManagerFields =
        {
            "timeScale", "timescale", "gameSpeed", "speed", "speedMultiplier",
            "globalTimeScale", "timeSpeed"
        };

        public void OnStart()
        {
            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _fastTimeScale = UnityEngine.Random.Range(3f, 6f);
            _suppressed = PauseStateService.IsMenuOpen();
            if (!_suppressed)
                ApplyTimeScale(_fastTimeScale, recordOriginals: true);
            NotificationService.Show("HYPER SPEED ACTIVATED!", null, NotificationService.NotificationType.Warning);
            MegaChaos.Main.Msg($"[HyperSpeed] timeScale={_fastTimeScale}");
        }

        public void OnPauseState(bool isTimePaused, bool isMenuOpen)
        {
            if (isMenuOpen)
            {
                if (!_suppressed)
                {
                    if (Time.timeScale > 0f)
                        ApplyTimeScale(_originalTimeScale, recordOriginals: false, restoreOriginals: true);
                    _suppressed = true;
                }
                return;
            }

            if (_suppressed)
            {
                ApplyTimeScale(_fastTimeScale, recordOriginals: false);
                _suppressed = false;
            }
        }
        public void OnUpdate(float dt)
        {
            if (_suppressed) return;
            if (Time.timeScale != _fastTimeScale && Time.timeScale > 0f)
                ApplyTimeScale(_fastTimeScale, recordOriginals: false);
        }
        public void OnGUI() { }

        public void OnEnd()
        {
            ApplyTimeScale(_originalTimeScale, recordOriginals: false, restoreOriginals: true);
            NotificationService.Show("Speed back to normal.", null, NotificationService.NotificationType.Reward);
        }

        private void ApplyTimeScale(float value, bool recordOriginals, bool restoreOriginals = false)
        {
            Time.timeScale = value;
            Time.fixedDeltaTime = _originalFixedDeltaTime * value;

            var manager = GetGameManagerInstance();
            if (manager == null) return;

            foreach (var field in ManagerFields)
            {
                try
                {
                    if (recordOriginals)
                    {
                        var current = GameReflection.GetMember(manager, field);
                        if (current is float f)
                            _managerOriginals[field] = f;
                    }

                    if (restoreOriginals && _managerOriginals.TryGetValue(field, out var original))
                    {
                        GameReflection.SetMember(manager, field, original);
                    }
                    else
                    {
                        GameReflection.SetMember(manager, field, value);
                    }
                }
                catch { }
            }
        }

        private object GetGameManagerInstance()
        {
            try
            {
                var gameManagerType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Managers.GameManager",
                    "Assets.Scripts.Managers.GameManager",
                    "GameManager",
                    "Il2CppGameManager");
                if (gameManagerType == null) return null;

                return GameReflection.GetStaticMember(gameManagerType, "Instance")
                       ?? GameReflection.InvokeStatic(gameManagerType, "get_Instance", Type.EmptyTypes);
            }
            catch
            {
                return null;
            }
        }
    }
}
