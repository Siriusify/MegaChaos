using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos
{
    public class ChaosEngine
    {
        public static ChaosEngine Instance { get; private set; } = new ChaosEngine();

        private ChaosEngine() { }

        private List<IChaosEffect> _availableEffects = new List<IChaosEffect>();
        public IReadOnlyList<IChaosEffect> AvailableEffects => _availableEffects;
        
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
            CameraEffectStack.Apply(); // tüm kamera delta'larını topla ve uygula
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

        private void UpdateActiveEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var state = _activeEffects[i];
                
                // Her frame çalışması gereken kodları (varsa) tetikle
                state.Effect.OnUpdate(Time.unscaledDeltaTime);

                // Süreli bir etkiyse süreyi düşür
                if (!state.IsPermanent)
                {
                    state.RemainingTime -= Time.unscaledDeltaTime;
                    if (state.RemainingTime <= 0)
                    {
                        // Etkinin süresi doldu, geri al (restore)
                        state.Effect.OnEnd();
                        _activeEffects.RemoveAt(i);
                        MegaChaos.Main.Msg($"[MegaChaos] Effect ended: {state.Effect.Name}");
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

            int randomIndex = Random.Range(0, _availableEffects.Count);
            IChaosEffect effectToStart = _availableEffects[randomIndex];
            TriggerEffect(effectToStart);
        }

        public void TriggerEffect(IChaosEffect effect, float customDuration = -2f)
        {
            float durationToUse = customDuration == -2f ? effect.DefaultDuration : customDuration;

            MegaChaos.Main.Msg($"[MegaChaos] Triggering Effect: {effect.Name} (Duration: {durationToUse})");
            MegaChaos.Services.NotificationService.Show($"CHAOS: {effect.Name}!", null, MegaChaos.Services.NotificationService.NotificationType.Warning);
            
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
            else if (durationToUse == -1) // Kalıcı etki
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
                // Anında biten etki
                effect.OnEnd();
            }
        }
    }
}
