using System;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class OneHPEffect : IChaosEffect
    {
        public string Id => "effect_onehp";
        public string Name => "Critical Condition";
        public string Description => "Your health suddenly drops to 1! The next hit could be fatal.";
        public float DefaultDuration => 0f; // Anlık efekt

        public void OnStart()
        {
            try
            {
                var playerType = GameReflection.FindType("Player");
                if (playerType != null)
                {
                    var instance = GameReflection.GetStaticMember(playerType, "Instance") 
                                   ?? GameReflection.GetStaticMember(playerType, "get_Instance");

                    if (instance != null)
                    {
                        var playerHealth = GameReflection.GetMember(instance, "playerHealth");
                        if (playerHealth != null)
                        {
                            var combined = GameReflection.InvokeInstance(playerHealth, "GetCombinedHp", Type.EmptyTypes);
                            if (combined != null)
                            {
                                float hp = Convert.ToSingle(combined);
                                if (hp > 1f)
                                {
                                    var method = playerHealth.GetType().GetMethod("DamagePlayerExternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (method != null)
                                    {
                                        method.Invoke(playerHealth, new object[] { hp - 1f, 0f, Vector3.zero, true, "MegaChaos_OneHP", 0, 0, null });
                                    }
                                    
                                    NotificationService.Show("CRITICAL CONDITION! 1 HP LEFT!", null, NotificationService.NotificationType.Unlucky);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Warn("[OneHPEffect] Error: " + ex.Message);
            }
            
            NotificationService.Show("Critical Condition Failed.", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
