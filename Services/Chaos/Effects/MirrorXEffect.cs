using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Mirrors the screen horizontally by flipping the camera's X scale.
    /// Uses GL.invertCulling to prevent the black-world issue caused by reversed winding order.
    /// </summary>
    public class MirrorXEffect : IChaosEffect
    {
        public string Id => "effect_mirrorx";
        public string Name => "Mirror World";
        public string Description => "Mirrors the screen horizontally! Controls still work normally but your eyes will be fooled.";
        public float DefaultDuration => 30f;

        private Camera _cam;
        private bool _active;

        public void OnStart()
        {
            _cam = Camera.main;
            if (_cam == null) return;

            // Flip the camera's local X scale.
            // GL.invertCulling fixes the inside-out / black-mesh issue that occurs
            // when the projection matrix determinant becomes negative.
            var s = _cam.transform.localScale;
            _cam.transform.localScale = new Vector3(-s.x, s.y, s.z);
            GL.invertCulling = true;
            _active = true;

            NotificationService.Show("Mirror World!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_cam != null && _active)
            {
                // Restore original X scale (flip back)
                var s = _cam.transform.localScale;
                _cam.transform.localScale = new Vector3(-s.x, s.y, s.z);
                GL.invertCulling = false;
                _active = false;
            }
            NotificationService.Show("Mirror World lifted!", null, NotificationService.NotificationType.Reward);
        }
    }
}
