using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// EXP Süpürge:
    /// Haritadaki tüm XP nesnelerini toplamak için oyunun kendi dahili
    /// PickupManager.Instance.PickupAllXp() metodunu tetikler.
    /// Bu sayede XP'ler oyuncuya doğru çekilir ve oyun içi standart toplama işlemi gerçekleşir.
    /// </summary>
    public class ExpCleanerEffect : IChaosEffect
    {
        public string Id => "effect_expcleaner";
        public string Name => "EXP Vacuum";
        public string Description => "Haritadaki tüm XP toplarını anında kendine çekersin!";
        public float DefaultDuration => 30f;

        private float _timer;
        private const float PullInterval = 0.5f;

        public void OnStart()
        {
            _timer = 0f;
            NotificationService.Show("EXP Vacuum activated! Pulling XP... 🌀", null, NotificationService.NotificationType.Reward);
        }

        public void OnUpdate(float dt)
        {
            _timer += dt;
            if (_timer >= PullInterval)
            {
                _timer = 0f;
                try
                {
                    var managerType = GameReflection.FindType("PickupManager");
                    if (managerType != null)
                    {
                        var instance = GameReflection.GetStaticMember(managerType, "Instance") 
                                       ?? GameReflection.GetStaticMember(managerType, "get_Instance");

                        if (instance != null)
                            GameReflection.InvokeInstance(instance, "PickupAllXp", Type.EmptyTypes);
                    }
                }
                catch { }
            }
        }

        public void OnGUI() { }

        public void OnEnd()
        {
            NotificationService.Show("EXP Vacuum ended.", null, NotificationService.NotificationType.Warning);
        }
    }
}
