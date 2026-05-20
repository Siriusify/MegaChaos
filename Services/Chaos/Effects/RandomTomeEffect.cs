using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    /// <summary>
    /// Tome Çekiliş:
    /// 1. TomeInventory.tomeUpgrade dict → Oyunun içinde tanımlı TomeData nesneleri
    /// 2. Eğer dict boşsa: Resources.FindObjectsOfTypeAll(TomeData) ile ScriptableObject'ları ara
    /// 3. Bulduğumuz TomeData'nın statModifier'ını permanent=true olarak StatInventory'ye uygula
    /// 4. TomeInventory.AddTome() ile tome arayüzüne de ekle
    /// 5. ForceUpdateStats() ile Stats UI'ı güncelle
    /// </summary>
    public class RandomTomeEffect : IChaosEffect
    {
        public string Id => "effect_randomtome";
        public string Name => "Tome Çekiliş";
        public string Description => "Rastgele kalıcı bir tome etkisi alırsın!";
        public float DefaultDuration => 0f; // anlık, kalıcı

        private static readonly System.Random _rng = new();

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player       = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory    = GameReflection.GetMember(player, "inventory");
                var tomeInv      = GameReflection.GetMember(inventory, "tomeInventory");
                var statInv      = GameReflection.GetMember(inventory, "statInventory");
                var playerStats  = GameReflection.GetMember(inventory, "playerStats");

                if (tomeInv == null) { NotificationService.Show("Tome sistemi bulunamadı.", null, NotificationService.NotificationType.Unlucky); return; }

                // TomeData bul
                object tomeData = FindTomeData(tomeInv, inventory);

                if (tomeData == null)
                {
                    NotificationService.Show("Tome Çekiliş: Tome verisi yüklenemedi.", null, NotificationService.NotificationType.Unlucky);
                    MegaChaos.Main.Warn("[RandomTome] TomeData bulunamadı.");
                    return;
                }

                // TomeData.statModifier → bu tome'un sağladığı stat bonusu
                var statModifier = GameReflection.GetMember(tomeData, "statModifier");

                if (statModifier != null && statInv != null)
                {
                    var statModType = statModifier.GetType();
                    // Kalıcı olarak uygula (permanent=true)
                    GameReflection.InvokeInstance(statInv, "ChangeStat",
                        new[] { statModType, typeof(bool), typeof(float), typeof(bool) },
                        statModifier, true, 0f, false);
                }

                // TomeInventory.AddTome ile UI'a da ekle
                try
                {
                    var eRarityType = GameReflection.FindType(
                        "Il2CppAssets.Scripts.Inventory__Items__Pickups.ERarity",
                        "Assets.Scripts.Inventory__Items__Pickups.ERarity", "ERarity");

                    object rarity = 0;
                    if (eRarityType != null)
                    {
                        var rarities = Enum.GetValues(eRarityType);
                        rarity = rarities.GetValue(_rng.Next(rarities.Length));
                    }

                    object upgradeList = null;
                    try { upgradeList = GameReflection.InvokeInstance(tomeData, "GetUpgradeOffer", new[] { eRarityType ?? typeof(int) }, rarity); } catch { }

                    GameReflection.InvokeInstance(tomeInv, "AddTome",
                        new[] { tomeData.GetType(), upgradeList?.GetType() ?? typeof(List<object>), eRarityType ?? typeof(int) },
                        tomeData, upgradeList, rarity);
                }
                catch (Exception ex) { MegaChaos.Main.Warn("[RandomTome] AddTome UI: " + ex.Message); }

                // Stats UI güncelle
                if (playerStats != null)
                    try { GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes); } catch { }

                // Tome adı
                object tomeName = null;
                try { tomeName = GameReflection.GetMember(tomeData, "eTome"); } catch { }
                try { if (tomeName == null) tomeName = GameReflection.InvokeInstance(tomeData, "GetName", Type.EmptyTypes); } catch { }

                NotificationService.Show($"+Tome: {tomeName ?? "???"} 📖", null, NotificationService.NotificationType.Reward);
                MegaChaos.Main.Msg($"[RandomTome] Tome eklendi: {tomeName}");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[RandomTome] " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private object FindTomeData(object tomeInv, object inventory)
        {
            var eTomeType = GameReflection.FindType(
                "Il2CppAssets.Scripts._Data.Tomes.ETome",
                "Assets.Scripts._Data.Tomes.ETome", "ETome");

            // Yol 1: tomeUpgrade dict
            try
            {
                var tomeUpgrade = GameReflection.GetMember(tomeInv, "tomeUpgrade") as IDictionary;
                if (tomeUpgrade != null && tomeUpgrade.Count > 0)
                {
                    var candidates = GetUnownedTomeData(tomeUpgrade, tomeInv, eTomeType);
                    if (candidates.Count > 0) return candidates[_rng.Next(candidates.Count)];
                }
            }
            catch (Exception ex) { MegaChaos.Main.Warn("[RandomTome] tomeUpgrade: " + ex.Message); }

            // Yol 2: statToTomes dict
            try
            {
                var statToTomes = GameReflection.GetMember(tomeInv, "statToTomes") as IDictionary;
                if (statToTomes != null && statToTomes.Count > 0)
                {
                    var list = new List<object>();
                    foreach (DictionaryEntry e in statToTomes)
                        if (e.Value != null) list.Add(e.Value);
                    if (list.Count > 0) return list[_rng.Next(list.Count)];
                }
            }
            catch (Exception ex) { MegaChaos.Main.Warn("[RandomTome] statToTomes: " + ex.Message); }

            // Yol 3: Resources.FindObjectsOfTypeAll(TomeData)
            try
            {
                var tomeDataType = GameReflection.FindType("TomeData");
                if (tomeDataType != null)
                {
                    var findMethod = typeof(UnityEngine.Resources).GetMethod(
                        "FindObjectsOfTypeAll",
                        BindingFlags.Static | BindingFlags.Public,
                        null, new[] { typeof(Type) }, null);

                    if (findMethod != null)
                    {
                        var results = findMethod.Invoke(null, new object[] { tomeDataType }) as IEnumerable;
                        if (results != null)
                        {
                            var list = new List<object>();
                            foreach (var obj in results) if (obj != null) list.Add(obj);
                            MegaChaos.Main.Msg($"[RandomTome] Resources yolu: {list.Count} TomeData bulundu");
                            if (list.Count > 0) return list[_rng.Next(list.Count)];
                        }
                    }
                }
            }
            catch (Exception ex) { MegaChaos.Main.Warn("[RandomTome] Resources: " + ex.Message); }

            return null;
        }

        private List<object> GetUnownedTomeData(IDictionary dict, object tomeInv, Type eTomeType)
        {
            var unowned = new List<object>();
            var all     = new List<object>();

            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value == null) continue;
                all.Add(entry.Value);

                if (eTomeType != null)
                {
                    try
                    {
                        var has = GameReflection.InvokeInstance(tomeInv, "HasTome", new[] { eTomeType }, entry.Key);
                        if (has == null || !(bool)has) unowned.Add(entry.Value);
                    }
                    catch { unowned.Add(entry.Value); }
                }
            }

            return unowned.Count > 0 ? unowned : all;
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }
}
