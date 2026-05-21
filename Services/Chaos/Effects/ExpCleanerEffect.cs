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
        public float DefaultDuration => 0f;

        public void OnStart()
        {
            try
            {
                var managerType = GameReflection.FindType("PickupManager");
                if (managerType != null)
                {
                    var instance = GameReflection.GetStaticMember(managerType, "Instance") 
                                   ?? GameReflection.GetStaticMember(managerType, "get_Instance");

                    if (instance != null)
                    {
                        GameReflection.InvokeInstance(instance, "PickupAllXp", Type.EmptyTypes);
                        NotificationService.Show("EXP Vacuum: Pulled all XP!", null, NotificationService.NotificationType.Reward);
                        return;
                    }
                }
            }
            catch { }
            
            NotificationService.Show("EXP Vacuum: Failed to pull.", null, NotificationService.NotificationType.Unlucky);
        }

        public void OnUpdate(float dt) { }

        public void OnGUI() { }

        public void OnEnd() { }
    }
}
