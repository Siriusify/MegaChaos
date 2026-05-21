using System;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Reverse Gravity — Flips Physics.gravity.y so the player floats upward.
    /// Distinct from Low Gravity (which just weakens it); this fully inverts it.
    /// </summary>
    public class ReverseGravityEffect : IChaosEffect
    {
        public string Id => "effect_reversegravity";
        public string Name => "Reverse Gravity";
        public string Description => "Gravity flips! Everything falls upward.";
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
                // Fully invert Y — player and physics objects fly upward
                SetGravity(new Vector3(_originalGravity.x, -_originalGravity.y, _originalGravity.z));
                _applied = true;
                NotificationService.Show("REVERSE GRAVITY! Everything falls up! ⬆️", null, NotificationService.NotificationType.Warning);
                Main.Msg("[ReverseGravity] gravity.y inverted.");
            }
            catch (Exception ex) { Main.Error("[ReverseGravity] " + ex.Message); }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            if (_applied) { SetGravity(_originalGravity); _applied = false; }
            NotificationService.Show("Gravity restored. Welcome back down.", null, NotificationService.NotificationType.Reward);
        }
    }
}
