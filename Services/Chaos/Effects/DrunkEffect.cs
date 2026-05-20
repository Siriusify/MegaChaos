using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.Services.Chaos.Effects
{
    public class DrunkEffect : IChaosEffect
    {
        public string Id => "effect_drunk";
        public string Name => "Sarhoşluk";
        public string Description => "Kamera sendeliyor, FOV dalgalanıyor, adım atmak çilesiz!";
        public float DefaultDuration => 12f;

        private float _elapsed;
        private float _fovAmp;
        private float _rollAmp;
        private float _swaySpeedX;
        private float _swaySpeedY;
        private float _swayAmpX;
        private float _swayAmpY;

        public void OnStart()
        {
            _elapsed     = 0f;
            _fovAmp      = Random.Range(5f, 14f);
            _rollAmp     = Random.Range(4f, 12f);
            _swaySpeedX  = Random.Range(1.2f, 2.5f);
            _swaySpeedY  = Random.Range(1.5f, 3.0f);
            _swayAmpX    = Random.Range(0.05f, 0.15f);
            _swayAmpY    = Random.Range(0.03f, 0.10f);
            NotificationService.Show("Başın dönüyor...", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime)
        {
            _elapsed += deltaTime;

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
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            NotificationService.Show("Ayıldın! Bir daha içme!", null, NotificationService.NotificationType.Reward);
        }
    }
}
