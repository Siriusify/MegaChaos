using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos
{
    public class ChaosLogEntry
    {
        public string Time { get; }
        public string EffectName { get; }

        public ChaosLogEntry(string effectName)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            EffectName = effectName;
        }
    }

    public class ChaosEngine
    {
        public static ChaosEngine Instance { get; private set; } = new ChaosEngine();

        private ChaosEngine() { }

        private List<IChaosEffect> _availableEffects = new List<IChaosEffect>();
        public IReadOnlyList<IChaosEffect> AvailableEffects => _availableEffects;

        private readonly List<ChaosLogEntry> _log = new List<ChaosLogEntry>();
        public IReadOnlyList<ChaosLogEntry> Log => _log;

        private class ActiveEffectState
        {
            public IChaosEffect Effect;
            public float RemainingTime;
            public bool IsPermanent;
        }
        
        private List<ActiveEffectState> _activeEffects = new List<ActiveEffectState>();

        public void Update()
        {
            UpdateActiveEffects();
            CameraEffectStack.Apply();
        }

        public void OnGUI()
        {
            foreach (var state in _activeEffects)
            {
                state.Effect.OnGUI();
            }
        }

        public void ClearAllEffects()
        {
            foreach (var state in _activeEffects)
            {
                state.Effect.OnEnd();
            }
            _activeEffects.Clear();
        }

        public void ClearLog()
        {
            _log.Clear();
        }

        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var state = _activeEffects[i];
                state.Effect.OnUpdate(Time.unscaledDeltaTime);

                if (!state.IsPermanent)
                {
                    state.RemainingTime -= Time.unscaledDeltaTime;
                    if (state.RemainingTime <= 0)
                    {
                        state.Effect.OnEnd();
                        _activeEffects.RemoveAt(i);
                    }
                }
            }
        }

        public void RegisterEffect(IChaosEffect effect)
        {
            if (!_availableEffects.Contains(effect))
            {
                _availableEffects.Add(effect);
                MegaChaos.Main.Msg($"[MegaChaos] Registered chaos effect: {effect.Name}");
            }
        }

        public void TriggerRandomEffect()
        {
            if (_availableEffects.Count == 0) return;
            int randomIndex = UnityEngine.Random.Range(0, _availableEffects.Count);
            TriggerEffect(_availableEffects[randomIndex]);
        }

        public void TriggerEffect(IChaosEffect effect, float customDuration = -2f)
        {
            var profile = ProfileManager.ActiveProfile;
            float multiplier = profile?.ChaosDurationMultiplier ?? 1f;
            float baseDuration = customDuration == -2f ? effect.DefaultDuration : customDuration;
            float durationToUse = baseDuration > 0 ? baseDuration * multiplier : baseDuration;

            MegaChaos.Main.Msg($"[MegaChaos] Triggering: {effect.Name} (Duration: {durationToUse:F1}s)");
            MegaChaos.Services.NotificationService.Show($"CHAOS: {effect.Name}!", null, MegaChaos.Services.NotificationService.NotificationType.Warning);

            // Add to log (keep last 100 entries)
            _log.Insert(0, new ChaosLogEntry(effect.Name));
            if (_log.Count > 100) _log.RemoveAt(_log.Count - 1);

            effect.OnStart();

            if (durationToUse > 0)
            {
                _activeEffects.Add(new ActiveEffectState
                {
                    Effect = effect,
                    RemainingTime = durationToUse,
                    IsPermanent = false
                });
            }
            else if (durationToUse == -1)
            {
                _activeEffects.Add(new ActiveEffectState
                {
                    Effect = effect,
                    RemainingTime = 0,
                    IsPermanent = true
                });
            }
            else
            {
                effect.OnEnd();
            }
        }
    }
}
