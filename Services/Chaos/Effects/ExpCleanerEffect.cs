using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// EXP Süpürge:
    /// Haritadaki tüm XP nesnelerini toplamak için oyunun kendi dahili
    /// PickupManager.Instance.PickupAllXp() metodunu tetikler.
    /// EXP Vacuum:
    /// Triggers the game's internal PickupManager.Instance.PickupAllXp() method 
    /// to collect all XP objects on the map. This pulls the XP towards the player 
    /// and triggers standard in-game collection logic.
    /// </summary>
    public class ExpCleanerEffect : IChaosEffect
    {
        public string Id => "effect_expcleaner";
        public string Name => "EXP Vacuum";
        public string Description => "Instantly pulls all XP orbs on the map to you!";
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
