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

        // Last 2 triggered effect IDs for cooldown
        private readonly Queue<string> _recentIds = new Queue<string>();
        private const int CooldownCount = 2;

        // ── Active effects ────────────────────────────────────────────────
        public class ActiveEffectState
        {
            public IChaosEffect Effect;
            public float TotalDuration;
            public float RemainingTime;
            public bool IsPermanent;
            // For overlay display: keep fading 3s after ending
            public float EndFadeTimer = -1f;
        }

        private readonly List<ActiveEffectState> _activeEffects = new List<ActiveEffectState>();

        // Overlay: last 3 completed OR active effects
        private readonly List<ActiveEffectState> _overlaySlots = new List<ActiveEffectState>();

        // ── GTA-style overlay styles (lazy init) ─────────────────────────
        private GUIStyle _overlayNameStyle;
        private GUIStyle _overlayFadeStyle;
        private Texture2D _barBgTex;
        private Texture2D _barFillTex;
        private bool _overlayStylesReady;

        private const float OverlayWidth  = 260f;
        private const float OverlayRowH   = 52f;
        private const float OverlayFadeTime = 3f;
        private const float OverlayRightMargin = 20f;
        private const float OverlayBarH   = 8f;

        public void Update()
        {
            UpdateActiveEffects();
        }

        public void LateUpdate()
        {
            CameraEffectStack.Apply();
        }

        public void OnGUI()
        {
            // Draw effect visuals
            foreach (var state in _activeEffects)
                state.Effect.OnGUI();

            // Draw GTA-style overlay
            DrawOverlay();
        }

        public void ClearAllEffects()
        {
            foreach (var state in _activeEffects)
                state.Effect.OnEnd();
            _activeEffects.Clear();
            _overlaySlots.Clear();
            _recentIds.Clear();
        }

        public void ClearLog() => _log.Clear();

        public void AddLogEntry(string message)
        {
            _log.Insert(0, new ChaosLogEntry(message));
            if (_log.Count > 100) _log.RemoveAt(_log.Count - 1);
        }

        private void UpdateActiveEffects()
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
                        var playerHealth = GameReflection.GetMember(inventory, "playerHealth");
                        if (playerHealth != null)
                        {
                            var currentHealthObj = GameReflection.GetMember(playerHealth, "currentHealth");
                            if (currentHealthObj is float fHealth && fHealth <= 0)
                            {
                                if (_activeEffects.Count > 0)
                                {
                                    ClearAllEffects();
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            float dt = Time.deltaTime;

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var state = _activeEffects[i];
                state.Effect.OnUpdate(dt);

                if (!state.IsPermanent)
                {
                    state.RemainingTime -= dt;
                    if (state.RemainingTime <= 0)
                    {
                        state.Effect.OnEnd();
                        // Start overlay fade-out
                        state.EndFadeTimer = OverlayFadeTime;
                        _activeEffects.RemoveAt(i);
                        // Move to overlay fading list
                        _overlaySlots.Add(state);
                        // Keep only last 3 slots
                        while (_overlaySlots.Count > 3)
                            _overlaySlots.RemoveAt(0);
                    }
                }
            }

            // Tick overlay fade timers
            for (int i = _overlaySlots.Count - 1; i >= 0; i--)
            {
                var s = _overlaySlots[i];
                if (s.EndFadeTimer >= 0)
                {
                    s.EndFadeTimer -= dt;
                    if (s.EndFadeTimer < 0) s.EndFadeTimer = 0;
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

            // Build pool excluding recently triggered effects
            var pool = new List<IChaosEffect>(_availableEffects.Count);
            foreach (var e in _availableEffects)
            {
                bool onCooldown = false;
                foreach (var id in _recentIds)
                    if (id == e.Id) { onCooldown = true; break; }
                if (!onCooldown) pool.Add(e);
            }

            // Fallback: if all are on cooldown, use full pool
            if (pool.Count == 0) pool = new List<IChaosEffect>(_availableEffects);

            TriggerEffect(pool[UnityEngine.Random.Range(0, pool.Count)]);
        }

        public void ExtendAllActive(float minMult, float maxMult)
        {
            float mult = UnityEngine.Random.Range(minMult, maxMult);
            foreach (var state in _activeEffects)
            {
                if (!state.IsPermanent && state.TotalDuration > 0)
                {
                    state.RemainingTime *= mult;
                    state.TotalDuration = Mathf.Max(state.TotalDuration, state.RemainingTime);
                }
            }
        }

        public void ClearActiveTimedEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var state = _activeEffects[i];
                if (!state.IsPermanent && state.TotalDuration > 0)
                {
                    state.Effect.OnEnd();
                    state.EndFadeTimer = OverlayFadeTime;
                    _overlaySlots.Add(state);
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        public void TriggerEffect(IChaosEffect effect, float customDuration = -2f)
        {
            var profile = ProfileManager.ActiveProfile;
            float multiplier = profile?.ChaosDurationMultiplier ?? 1f;
            float baseDuration = customDuration == -2f ? effect.DefaultDuration : customDuration;
            float durationToUse = baseDuration > 0 ? baseDuration * multiplier : baseDuration;

            MegaChaos.Main.Msg($"[MegaChaos] Triggering: {effect.Name} (Duration: {durationToUse:F1}s)");
            MegaChaos.Services.NotificationService.Show($"CHAOS: {effect.Name}!", null,
                MegaChaos.Services.NotificationService.NotificationType.Warning);

            // Cooldown tracking
            _recentIds.Enqueue(effect.Id);
            while (_recentIds.Count > CooldownCount) _recentIds.Dequeue();

            // Log entry
            _log.Insert(0, new ChaosLogEntry(effect.Name));
            if (_log.Count > 100) _log.RemoveAt(_log.Count - 1);

            // Check if already active (stack duration)
            var existing = _activeEffects.Find(s => s.Effect.Id == effect.Id);
            if (existing != null)
            {
                if (durationToUse > 0)
                {
                    existing.RemainingTime += durationToUse;
                    existing.TotalDuration = Mathf.Max(existing.TotalDuration, existing.RemainingTime);
                }
                effect.OnStart(); // Trigger again for initial impact
                
                // Move to top of active list (make it newest)
                _activeEffects.Remove(existing);
                _activeEffects.Insert(0, existing);
                return;
            }

            effect.OnStart();

            if (durationToUse > 0)
            {
                var state = new ActiveEffectState
                {
                    Effect = effect,
                    TotalDuration = durationToUse,
                    RemainingTime = durationToUse,
                    IsPermanent = false
                };
                _activeEffects.Insert(0, state); // Newest at top
            }
            else if (durationToUse == -1)
            {
                var state = new ActiveEffectState
                {
                    Effect = effect,
                    TotalDuration = -1,
                    RemainingTime = 0,
                    IsPermanent = true
                };
                _activeEffects.Insert(0, state);
            }
            else
            {
                // Instant effect
                var state = new ActiveEffectState
                {
                    Effect = effect,
                    TotalDuration = 0,
                    RemainingTime = 0,
                    IsPermanent = false,
                    EndFadeTimer = OverlayFadeTime
                };
                effect.OnEnd();
                _overlaySlots.Insert(0, state);
            }
        }

        // ── GTA-style overlay ─────────────────────────────────────────────
        private void DrawOverlay()
        {
            if (_overlaySlots.Count == 0 && _activeEffects.Count == 0) return;

            if (!_overlayStylesReady || _barBgTex == null || _barFillTex == null) InitOverlayStyles();
            if (!_overlayStylesReady) return;

            // Build display list: newest active effects first, then fading slots
            var display = new List<ActiveEffectState>();
            foreach (var s in _activeEffects)
                if (!display.Contains(s)) display.Add(s);
            foreach (var s in _overlaySlots)
                if (!display.Contains(s)) display.Add(s);

            float screenW = Screen.width > 100 ? Screen.width : 1920f;
            float screenH = Screen.height > 100 ? Screen.height : 1080f;
            float startX = screenW - OverlayWidth - OverlayRightMargin;
            float startY = screenH / 2f - (display.Count * OverlayRowH) / 2f;

            var savedColor = GUI.color;

            for (int i = 0; i < display.Count; i++)
            {
                var s = display[i];
                bool isActive = s.EndFadeTimer < 0; // still running
                bool isFading = !isActive && s.EndFadeTimer >= 0;
                bool isPermanent = isActive && s.IsPermanent;
                bool hideBar = s.Effect.Id.StartsWith("effect_fake") || s.Effect.Id == "effect_cleareffects"; // Fake effects and clear effect hide bar

                float alpha = 1f;
                if (isFading)
                    alpha = Mathf.Clamp01(s.EndFadeTimer / OverlayFadeTime);

                float rowY = startY + i * OverlayRowH;

                // ── Dark Background for readability ──────────────────────
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.6f * alpha);
                GUI.DrawTexture(new Rect(startX - 10f, rowY - 5f, OverlayWidth + 20f, OverlayRowH + 10f), _barBgTex);

                // ── Name ─────────────────────────────────
                var nameStyle = isFading ? _overlayFadeStyle : _overlayNameStyle;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(startX, rowY, OverlayWidth, OverlayRowH - OverlayBarH - 2f),
                    s.Effect.Name, nameStyle);

                // ── Progress bar ──────────────────────────
                if (isActive && !isPermanent && s.TotalDuration > 0 && !hideBar)
                {
                    float progress = Mathf.Clamp01(s.RemainingTime / s.TotalDuration);
                    float barY = rowY + OverlayRowH - OverlayBarH - 2f;
                    float barW = OverlayWidth - 4f;

                    // Background
                    GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f * alpha);
                    GUI.DrawTexture(new Rect(startX, barY, barW, OverlayBarH), _barBgTex);

                    // Fill
                    Color fillCol = Color.Lerp(new Color(0.9f, 0.3f, 0.1f), new Color(0.1f, 0.9f, 0.4f), progress);
                    fillCol.a = alpha;
                    GUI.color = fillCol;
                    GUI.DrawTexture(new Rect(startX, barY, barW * progress, OverlayBarH), _barFillTex);
                }
            }

            GUI.color = savedColor;
        }

        private void InitOverlayStyles()
        {
            _overlayNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };
            _overlayNameStyle.normal.textColor = Color.white;

            _overlayFadeStyle = new GUIStyle(_overlayNameStyle);
            _overlayFadeStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);

            _barBgTex = new Texture2D(1, 1);
            _barBgTex.SetPixel(0, 0, Color.white);
            _barBgTex.Apply();

            _barFillTex = new Texture2D(1, 1);
            _barFillTex.SetPixel(0, 0, Color.white);
            _barFillTex.Apply();

            _overlayStylesReady = true;
        }
    }
}
