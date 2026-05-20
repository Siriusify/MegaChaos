using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Ekranı yatay aynalar. Sadece projeksiyon matrisinin X sütunu negatife çevrilerek
    /// siyah ekran sorunu olmadan çalışır.
    /// </summary>
    public class MirrorXEffect : IChaosEffect
    {
        public string Id => "effect_mirrorx";
        public string Name => "Ayna Dünya";
        public string Description => "Ekranı yatay olarak aynalar! Kontroller düz ama gözün yanılır.";
        public float DefaultDuration => 10f;

        private Camera _cam;
        private Matrix4x4 _originalMatrix;
        private bool _active;

        public void OnStart()
        {
            _cam = Camera.main;
            if (_cam != null)
            {
                _originalMatrix = _cam.projectionMatrix;
                var m = _originalMatrix;
                // Sadece X sütununu negatife çevir — Y ve W değişmez, siyah ekran olmaz
                m[0, 0] = -m[0, 0];
                m[0, 1] = -m[0, 1];
                m[0, 2] = -m[0, 2];
                m[0, 3] = -m[0, 3];
                _cam.projectionMatrix = m;
                _active = true;
            }
            NotificationService.Show("Dünya aynaya döndü!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_cam != null && _active)
            {
                _cam.projectionMatrix = _originalMatrix;
                _active = false;
            }
            NotificationService.Show("Ayna kalktı, dünya normale döndü!", null, NotificationService.NotificationType.Reward);
        }
    }
}
