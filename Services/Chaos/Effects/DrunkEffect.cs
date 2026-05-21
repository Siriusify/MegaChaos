using System;
using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class DrunkEffect : IChaosEffect
    {
        public string Id => "effect_drunk";
        public string Name => "Drunkness";
        public string Description => "The camera staggers and the FOV waves — walking is a chore!";
        public float DefaultDuration => 30f;

        private float _elapsed;
        private float _fovAmp;
        private float _rollAmp;
        private float _swaySpeedX;
        private float _swaySpeedY;
        private float _swayAmpX;
        private float _swayAmpY;

        private object _playerMovement;
        private object _playerRb;
        private float _stumbleTimer;

        public void OnStart()
        {
            _elapsed     = 0f;
            _fovAmp      = UnityEngine.Random.Range(5f, 14f);
            _rollAmp     = UnityEngine.Random.Range(4f, 12f);
            _swaySpeedX  = UnityEngine.Random.Range(1.2f, 2.5f);
            _swaySpeedY  = UnityEngine.Random.Range(1.5f, 3.0f);
            _swayAmpX    = UnityEngine.Random.Range(0.05f, 0.15f);
            _swayAmpY    = UnityEngine.Random.Range(0.03f, 0.10f);
            
            _playerMovement = null;
            _playerRb = null;
            _stumbleTimer = 0f;

            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                if (player != null)
                {
                    _playerMovement = GameReflection.GetMember(player, "playerMovement");
                    if (_playerMovement != null)
                    {
                        var rbObj = GameReflection.GetMember(_playerMovement, "rb");
                        if (rbObj != null) _playerRb = rbObj;
                    }
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[DrunkEffect] OnStart Error: " + ex.Message);
            }

            NotificationService.Show("Your head is spinning...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime)
        {
            if (Time.timeScale <= 0.01f) return;

            _elapsed += deltaTime;

            // Visual Drunkness
            float fovOffset = Mathf.Sin(_elapsed * 1.3f) * _fovAmp;
            float roll      = Mathf.Sin(_elapsed * 0.8f) * _rollAmp;
            float swayX     = Mathf.Sin(_elapsed * _swaySpeedX) * _swayAmpX;
            float swayY     = Mathf.Cos(_elapsed * _swaySpeedY) * _swayAmpY;

            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta
            {
                FovOffset  = fovOffset,
                PosOffset  = new Vector3(swayX, swayY, 0f),
                RollDeg    = roll
            });

            // Physical Drunkness (Apply velocity directly if AddForce is ignored)
            if (_playerRb != null && _playerMovement != null)
            {
                _stumbleTimer -= deltaTime;
                if (_stumbleTimer <= 0f)
                {
                    _stumbleTimer = UnityEngine.Random.Range(0.2f, 0.8f);

                    Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized;
                    // Try setting velocity directly since AddForce might be overwritten by custom player physics
                    try
                    {
                        var velProp = _playerRb.GetType().GetProperty("velocity");
                        if (velProp != null)
                        {
                            Vector3 currentVel = (Vector3)velProp.GetValue(_playerRb);
                            // Add chaotic stumble velocity
                            Vector3 newVel = currentVel + new Vector3(randomCircle.x * 12f, 0f, randomCircle.y * 12f);
                            if (UnityEngine.Random.value > 0.85f) newVel.y += 8f; // random hop
                            velProp.SetValue(_playerRb, newVel);
                        }
                    }
                    catch { }
                }

                // Mess with orientation to make walking straight impossible
                try
                {
                    var orientationObj = GameReflection.GetMember(_playerMovement, "orientation");
                    if (orientationObj != null && orientationObj is Transform orientTrans)
                    {
                        orientTrans.Rotate(0f, Mathf.Sin(_elapsed * 2f) * 2f, 0f);
                    }
                }
                catch { }
            }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("You sobered up! Don't drink again!", null, NotificationService.NotificationType.Reward);
        }
    }
}
