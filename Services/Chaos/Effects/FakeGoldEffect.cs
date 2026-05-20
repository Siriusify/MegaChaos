using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MegaChaos.Services.Chaos.Effects
{
    public class FakeGoldEffect : IChaosEffect
    {
        public string Id => "effect_fakegold";
        public string Name => "Tax Audit (Troll)";
        public string Description => "Eşyalarınızı ve altınlarınızı gerçekten hacveder, süre bitince iade eder!";
        public float DefaultDuration => 5f;

        private int _originalGold;
        private int _goldSeized;
        private Dictionary<object, int> _backedUpItems = new();
        private object _playerInventory;
        private object _itemInventory;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                _playerInventory = GameReflection.GetMember(player, "inventory");
                _itemInventory = GameReflection.GetMember(_playerInventory, "itemInventory");

                _originalGold = RunStatService.GetGold();
                
                // 1. Altını rastgele %50-%95 arasında azalt (100'den fazlaysa)
                if (_originalGold >= 100)
                {
                    float seizePct = UnityEngine.Random.Range(0.50f, 0.95f);
                    _goldSeized = (int)(_originalGold * seizePct);
                    GameReflection.InvokeInstance(_playerInventory, "ChangeGold", new[] { typeof(int) }, -_goldSeized);
                    MegaChaos.Main.Msg($"[MegaChaos] Vergi: {_originalGold} gold'un %{(int)(seizePct*100)}'i ({_goldSeized}) haciz edildi.");
                }
                else
                {
                    _goldSeized = 0;
                    MegaChaos.Main.Msg($"[MegaChaos] Vergi: Gold 100'den az ({_originalGold}), gold haczi atlandı.");
                }

                // 2. Eşyaları yedekle ve kaldır
                // EItem enum değerlerini C# tarafında iterate ediyoruz — bu sayede
                // IL2CPP sarmalama sorunu olmadan doğru tip eşleşmesi sağlanıyor.
                _backedUpItems.Clear();

                var eItemType = GameReflection.FindType(
                    "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                    "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
                    "EItem");

                if (eItemType != null)
                {
                    foreach (var enumVal in Enum.GetValues(eItemType))
                    {
                        object countObj;
                        try { countObj = GameReflection.InvokeInstance(_itemInventory, "GetAmount", new[] { eItemType }, enumVal); }
                        catch { continue; }

                        if (countObj == null) continue;
                        int count = Convert.ToInt32(countObj);
                        if (count <= 0) continue;

                        _backedUpItems[enumVal] = count;
                        MegaChaos.Main.Msg($"[MegaChaos] Haciz: {enumVal} x{count}");

                        // RemoveItem(EItem eItem, bool removeAll=false) → her çağrı 1 adet siler
                        for (int i = 0; i < count; i++)
                        {
                            try { GameReflection.InvokeInstance(_itemInventory, "RemoveItem", new[] { eItemType, typeof(bool) }, enumVal, false); }
                            catch { break; }
                        }
                    }
                }

                NotificationService.Show($"-{_originalGold} Gold (Vergi Cezası Kesildi!)", null, NotificationService.NotificationType.Unlucky);
                NotificationService.Show("Haciz İşlemi: Bütün eşyalarınıza el konuldu!", null, NotificationService.NotificationType.Warning);
                MegaChaos.Main.Msg($"[MegaChaos] Vergi Denetimi başladı. {_originalGold} gold ve {_backedUpItems.Count} farklı eşya türü haczedildi.");
            }
            catch (System.Exception ex)
            {
                MegaChaos.Main.Error("OnStart Hatası: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
        
        public void OnUpdate(float deltaTime) { }
        public void OnGUI() { }
        
        public void OnEnd() 
        { 
            try
            {
                if (_playerInventory != null && _goldSeized > 0)
                {
                    // 1. El konulan altını iade et
                    GameReflection.InvokeInstance(_playerInventory, "ChangeGold", new[] { typeof(int) }, _goldSeized);
                }

                if (_itemInventory != null)
                {
                    var eItemType = GameReflection.FindType(
                        "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
                        "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
                        "EItem");

                    // 2. Eşyaları iade et
                    foreach (var pair in _backedUpItems)
                    {
                        GameReflection.InvokeInstance(_itemInventory, "AddItem", new[] { eItemType, typeof(int) }, pair.Key, pair.Value);
                    }
                }

                _backedUpItems.Clear();

                NotificationService.Show("Şaka şaka, paran ve eşyaların yerinde duruyor!", null, NotificationService.NotificationType.Reward);
                MegaChaos.Main.Msg("[MegaChaos] Vergi Denetimi bitti. Altınlar ve eşyalar iade edildi.");
            }
            catch (System.Exception ex)
            {
                MegaChaos.Main.Error("OnEnd Hatası: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
