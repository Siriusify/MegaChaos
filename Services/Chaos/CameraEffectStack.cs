using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos
{
    /// <summary>
    /// Tüm kamera efektleri için merkezi koordinasyon noktası.
    /// Efektler mutlak değer yazmak yerine delta kayıt eder.
    /// Her frame: baseCam + toplam_delta uygulanır.
    /// Böylece birden fazla efekt çakışmadan üst üste çalışır.
    /// </summary>
    public static class CameraEffectStack
    {
        // ---- Tip Tanımları ----
        public class CameraDelta
        {
            public float FovOffset;       // Ek FOV (negatif = yakınlaştır)
            public float FovMultiplier;   // FOV çarpanı (1 = değişim yok)
            public Vector3 PosOffset;     // Local position eklemesi
            public float RollDeg;         // Z-roll açısı (derece)

            public CameraDelta()
            {
                FovMultiplier = 1f;
            }
        }

        // ---- State ----
        private static Camera _cam;
        private static bool _baseCapured;
        private static float _baseFov;
        private static float _baseOrtho;
        private static Vector3 _basePos;
        private static Quaternion _baseRot;

        private static readonly Dictionary<string, CameraDelta> _deltas = new();

        // ---- API ----
        public static void Register(string effectId, CameraDelta delta)
        {
            EnsureBase();
            _deltas[effectId] = delta;
        }

        public static void Unregister(string effectId)
        {
            _deltas.Remove(effectId);
            // Hiç efekt kalmadıysa base'e sıfırla
            if (_deltas.Count == 0)
                ResetToBase();
        }

        /// <summary>
        /// ChaosEngine.Update tarafından her frame çağrılır.
        /// </summary>
        public static void Apply()
        {
            if (!_baseCapured) return;
            if (_cam == null || _deltas.Count == 0) return;

            float totalFovOffset = 0f;
            float totalFovMult   = 1f;
            Vector3 totalPos     = Vector3.zero;
            float totalRoll      = 0f;

            foreach (var d in _deltas.Values)
            {
                totalFovOffset += d.FovOffset;
                totalFovMult   *= d.FovMultiplier;
                totalPos       += d.PosOffset;
                totalRoll      += d.RollDeg;
            }

            if (_cam.orthographic)
            {
                // Orthographic kamerada FOV yerine Size değişir.
                // _baseFov burada aslında base orthographicSize'ı temsil etmeli,
                // ama biz onu EnsureBase() içinde fieldOfView olarak almıştık.
                // O yüzden EnsureBase'i ve burayı orthographic destekleyecek şekilde güncelledik.
                _cam.orthographicSize = (_baseOrtho + totalFovOffset / 5f) * totalFovMult;
            }
            else
            {
                _cam.fieldOfView = (_baseFov + totalFovOffset) * totalFovMult;
            }
            
            _cam.transform.localPosition = _basePos + totalPos;
            _cam.transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, totalRoll);
        }

        public static bool IsEmpty => _deltas.Count == 0;

        // ---- Internal ----
        private static void EnsureBase()
        {
            if (_baseCapured) return;
            _cam = Camera.main;
            if (_cam == null) return;
            _baseFov       = _cam.fieldOfView;
            _baseOrtho     = _cam.orthographicSize;
            _basePos       = _cam.transform.localPosition;
            _baseRot       = _cam.transform.localRotation;
            _baseCapured   = true;
        }

        private static void ResetToBase()
        {
            if (_cam == null || !_baseCapured) return;
            _cam.fieldOfView             = _baseFov;
            _cam.orthographicSize        = _baseOrtho;
            _cam.transform.localPosition = _basePos;
            _cam.transform.localRotation = _baseRot;
        }

        /// <summary>
        /// Oyun yeniden başladığında base'i sıfırla (MapController patch üzerinden çağrılır).
        /// </summary>
        public static void InvalidateBase()
        {
            _baseCapured = false;
            _cam         = null;
            _deltas.Clear();
        }
    }
}
