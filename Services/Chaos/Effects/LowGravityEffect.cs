using System;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class LowGravityEffect : IChaosEffect
    {
        public string Id => "effect_lowgravity";
        public string Name => "Low Gravity";
        public string Description => "Neredeyse sıfır yerçekimi — uçuyor gibi hissedersin!";
        public float DefaultDuration => 30f;

        private Vector3 _originalGravity;
        private bool _applied;

        private static Vector3 GetGravity()
        {
            var t = Type.GetType("UnityEngine.Physics, UnityEngine.PhysicsModule")
                 ?? Type.GetType("UnityEngine.Physics");
            if (t == null) return new Vector3(0f, -9.81f, 0f);
            var prop = t.GetProperty("gravity", BindingFlags.Public | BindingFlags.Static);
            return prop != null ? (Vector3)prop.GetValue(null) : new Vector3(0f, -9.81f, 0f);
        }

        private static void SetGravity(Vector3 g)
        {
            var t = Type.GetType("UnityEngine.Physics, UnityEngine.PhysicsModule")
                 ?? Type.GetType("UnityEngine.Physics");
            if (t == null) return;
            var prop = t.GetProperty("gravity", BindingFlags.Public | BindingFlags.Static);
            prop?.SetValue(null, g);
        }

        public void OnStart()
        {
            _applied = false;
            try
            {
                _originalGravity = GetGravity();
                float mult = UnityEngine.Random.Range(0.05f, 0.20f);
                SetGravity(new Vector3(0f, _originalGravity.y * mult, 0f));
                _applied = true;
                NotificationService.Show($"LOW GRAVITY! x{mult:F2} — Welcome to the Moon 🌙", null, NotificationService.NotificationType.Warning);
                MegaChaos.Main.Msg($"[LowGravity] gravity y={_originalGravity.y * mult:F2}");
            }
            catch (Exception ex) { MegaChaos.Main.Error("[LowGravity] " + ex.Message); }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_applied) { SetGravity(_originalGravity); _applied = false; }
            NotificationService.Show("Gravity back to normal!", null, NotificationService.NotificationType.Reward);
        }
    }
}
