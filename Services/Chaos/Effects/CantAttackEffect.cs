using System;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// WeaponInventory'nin ateşleme döngüsünü geçici olarak durdurur.
    /// "canAttack" / "pause" field'i set ederek saldırıyı kilitler.
    /// </summary>
    public class CantAttackEffect : IChaosEffect
    {
        public string Id => "effect_cantattack";
        public string Name => "Pacifist";
        public string Description => "Silahların bir süreliğine çalışmıyor — kaçmaktan başka seçeneğin yok!";
        public float DefaultDuration => 30f;

        private object _weaponInventory;
        private bool _applied;

        public void OnStart()
        {
            _applied = false;
            try
            {
                var myPlayerType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                    "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory = GameReflection.GetMember(player, "inventory");
                _weaponInventory = GameReflection.GetMember(inventory, "weaponInventory");

                if (_weaponInventory == null)
                {
                    NotificationService.Show("Saldıramama: Silah sistemi bulunamadı.", null, NotificationService.NotificationType.Unlucky);
                    return;
                }

                // WeaponInventory'deki "pause" field'ini set et
                // PlayerInventory üzerinde de "pause" field'i var — ikisini de set edelim
                GameReflection.SetMember(_weaponInventory, "pause", true);
                GameReflection.SetMember(inventory, "pause", true);
                _applied = true;

                NotificationService.Show("SİLAHLARIN ÇALIŞMIYOR! Kaç kaç!", null, NotificationService.NotificationType.Unlucky);
                MegaChaos.Main.Msg("[CantAttack] Silahlar durduruldu.");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[CantAttack] OnStart: " + ex.Message);
            }
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            try
            {
                if (_weaponInventory != null && _applied)
                {
                    var myPlayerType = GameReflection.FindType(
                        "Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                        "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                    var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                    var inventory = GameReflection.GetMember(player, "inventory");
                    GameReflection.SetMember(_weaponInventory, "pause", false);
                    GameReflection.SetMember(inventory, "pause", false);
                    _applied = false;
                }
                NotificationService.Show("Silahların tekrar çalışıyor!", null, NotificationService.NotificationType.Reward);
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[CantAttack] OnEnd: " + ex.Message);
            }
        }
    }
}
