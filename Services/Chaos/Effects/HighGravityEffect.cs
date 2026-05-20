using System;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class HighGravityEffect : IChaosEffect
    {
        public string Id => "effect_highgravity";
        public string Name => "High Gravity";
        public string Description => "Yerçekimi çılgınca artar — zıplamak imkansıza döner!";
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
                float mult = UnityEngine.Random.Range(4f, 8f);
                SetGravity(new Vector3(0f, _originalGravity.y * mult, 0f));
                _applied = true;
                NotificationService.Show($"AĞIR YERÇEK! x{mult:F1} — Zıplamayı dene 😂", null, NotificationService.NotificationType.Warning);
                MegaChaos.Main.Msg($"[HighGravity] gravity y={_originalGravity.y * mult:F1}");
            }
            catch (Exception ex) { MegaChaos.Main.Error("[HighGravity] " + ex.Message); }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_applied) { SetGravity(_originalGravity); _applied = false; }
            NotificationService.Show("Yerçekimi normale döndü!", null, NotificationService.NotificationType.Reward);
        }
    }
}
