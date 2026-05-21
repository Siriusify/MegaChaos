using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class UpsideDownEffect : IChaosEffect
    {
        public string Id => "effect_upsidedown";
        public string Name => "Upside Down";
        public string Description => "Oyun dünyası tamamen ters yüz olur, kontrolleriniz birbirine girer!";
        public float DefaultDuration => 30f;
        
        public void OnStart()
        {
            // Kamera Stack üzerinden Z ekseninde 180 derece dönüş (Roll) uygula
            CameraEffectStack.Register(Id, new CameraEffectStack.CameraDelta { RollDeg = 180f });
            MegaChaos.Main.Msg("[MegaChaos] World turned upside down!");
            NotificationService.Show("UPSIDE DOWN!", null, NotificationService.NotificationType.Warning);
        }
        
        public void OnUpdate(float deltaTime) { }
        
        public void OnGUI() { }
        
        public void OnEnd()
        {
            CameraEffectStack.Unregister(Id);
            MegaChaos.Main.Msg("[MegaChaos] World back to normal.");
            NotificationService.Show("Upside Down ended.", null, NotificationService.NotificationType.Reward);
        }
    }
}
