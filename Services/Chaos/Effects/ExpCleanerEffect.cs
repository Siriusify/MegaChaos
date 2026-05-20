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
        public float DefaultDuration => 0f; // anlık

        public void OnStart()
        {
            try
            {
                var managerType = GameReflection.FindType("PickupManager");
                if (managerType == null)
                {
                    NotificationService.Show("EXP Süpürge: Sistem bulunamadı.", null, NotificationService.NotificationType.Unlucky);
                    return;
                }

                var instance = GameReflection.GetStaticMember(managerType, "Instance");
                if (instance == null)
                {
                    // Fallback to get_Instance prop just in case
                    instance = GameReflection.GetStaticMember(managerType, "get_Instance");
                }

                if (instance != null)
                {
                    GameReflection.InvokeInstance(instance, "PickupAllXp", Type.EmptyTypes);
                    NotificationService.Show("EXP Süpürge: Tüm XP'ler sana doğru çekiliyor! 🌀", null, NotificationService.NotificationType.Reward);
                    MegaChaos.Main.Msg("[ExpCleaner] Invoked PickupManager.Instance.PickupAllXp successfully.");
                }
                else
                {
                    NotificationService.Show("EXP Süpürge: Aktif yönetici bulunamadı.", null, NotificationService.NotificationType.Unlucky);
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[ExpCleaner] Hata: " + ex.Message + "\n" + ex.StackTrace);
                NotificationService.Show("EXP Süpürge başarısız oldu.", null, NotificationService.NotificationType.Unlucky);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
