using System;
using System.Collections.Generic;
using UnityEngine;

// Oyunun kendi HUD bileşenlerine doğrudan erişim (Assembly-CSharp referansı)
using Assets.Scripts.UI.InGame.Rewards;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// HUD Yok:
    /// Oyunun kesin HUD MonoBehaviour tiplerini (InventoryHud, ItemsHud, XpAndGoldHUD)
    /// doğrudan disabled eder. LevelupScreen, UpgradePicker gibi kritik ekranlar asla etkilenmez.
    /// </summary>
    public class NoHudEffect : IChaosEffect
    {
        public string Id => "effect_nohud";
        public string Name => "No HUD";
        public string Description => "Can, XP, altın barları kaybolur — upgrade ekranları açık kalır!";
        public float DefaultDuration => 30f;

        // Kapatılacak bileşen tipleri (Assembly-CSharp'tan direkt)
        // Reflection kullanmak yerine tip isimleri string olarak tutulur,
        // GameReflection ile runtime'da bulunur.
        private static readonly string[] HudTypeNames =
        {
            "InventoryHud",
            "ItemsHud",
            "XpAndGoldHUD",
        };

        private readonly List<(Behaviour comp, bool wasEnabled)> _saved = new();

        public void OnStart()
        {
            _saved.Clear();
            int count = 0;

            foreach (var typeName in HudTypeNames)
            {
                try
                {
                    var hudType = GameReflection.FindType(typeName);
                    if (hudType == null)
                    {
                        MegaChaos.Main.Warn($"[NoHud] Type not found: {typeName}");
                        continue;
                    }

                    // Non-generic overload — IL2CPP uyumlu
                    var findMethod = typeof(UnityEngine.Object).GetMethod(
                        "FindObjectsOfType",
                        new[] { typeof(Type) });

                    if (findMethod == null)
                    {
                        MegaChaos.Main.Warn("[NoHud] FindObjectsOfType(Type) method not found.");
                        continue;
                    }

                    var result = findMethod.Invoke(null, new object[] { hudType });
                    if (result == null) continue;

                    var arr = result as System.Array;
                    if (arr == null) continue;

                    foreach (var obj in arr)
                    {
                        if (obj == null) continue;

                        // Behaviour.enabled property'si kalıtsal — her MonoBehaviour'da bulunur
                        var behaviour = obj as Behaviour;
                        if (behaviour == null) continue;

                        if (behaviour.enabled)
                        {
                            behaviour.enabled = false;
                            _saved.Add((behaviour, true));
                            count++;
                            MegaChaos.Main.Msg($"[NoHud] Disabled: {typeName} on GO '{behaviour.gameObject.name}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MegaChaos.Main.Warn($"[NoHud] Error processing {typeName}: {ex.Message}");
                }
            }

            if (count > 0)
            {
                NotificationService.Show($"HUD Yok! ({count} bileşen gizlendi)", null, NotificationService.NotificationType.Warning);
                MegaChaos.Main.Msg($"[NoHud] Total disabled: {count}");
            }
            else
            {
                // Bu sahnede HUD bileşeni yok (örn. ana menü) — hata değil, normal
                NotificationService.Show("HUD Yok: Bu sahnede HUD yok.", null, NotificationService.NotificationType.Unlucky);
                MegaChaos.Main.Warn("[NoHud] No HUD components found in current scene (may be main menu).");
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            foreach (var (comp, wasEnabled) in _saved)
            {
                try
                {
                    if (comp != null) comp.enabled = wasEnabled;
                }
                catch { }
            }
            _saved.Clear();

            NotificationService.Show("HUD geri geldi!", null, NotificationService.NotificationType.Reward);
            MegaChaos.Main.Msg("[NoHud] All HUD components restored.");
        }
    }
}
