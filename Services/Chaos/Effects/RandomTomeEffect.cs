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
        public string Name => "Tome Lottery";
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

                if (tomeInv == null) { NotificationService.Show("Tome system not found.", null, NotificationService.NotificationType.Unlucky); return; }

                // TomeData bul
                object tomeData = FindTomeData(tomeInv, inventory);

                if (tomeData == null)
                {
                    NotificationService.Show("Tome Lottery: Failed to load tome data.", null, NotificationService.NotificationType.Unlucky);
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

            // Yol 3: GameReflection.FindObjectsOfTypeAll
            try
            {
                var tomeDataType = GameReflection.FindType("TomeData");
                if (tomeDataType != null)
                {
                    var allObjs = GameReflection.FindObjectsOfTypeAll(tomeDataType);
                    if (allObjs != null)
                    {
                        var list = new System.Collections.Generic.List<object>();
                        foreach (var obj in allObjs) if (obj != null) list.Add(obj);
                        MegaChaos.Main.Msg($"[RandomTome] Resources yolu: {list.Count} TomeData bulundu");
                        if (list.Count > 0) return list[_rng.Next(list.Count)];
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

    /// <summary>
    /// Fake Tome Lottery — Gives a tome but removes its stat bonus after 5 seconds.
    /// Appears as "Tome Lottery" to the player.
    /// </summary>
    public class FakeTomeLotteryEffect : IChaosEffect
    {
        public string Id => "effect_faketome";
        public string Name => "Tome Lottery";
        public string Description => "Gives a tome… then takes it back after 5 seconds!";
        public float DefaultDuration => 5f;

        private object _appliedStatModifier;
        private object _statInv;
        private object _playerStats;
        private string _tomeName;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer",
                    "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player      = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory   = GameReflection.GetMember(player, "inventory");
                var tomeInv     = GameReflection.GetMember(inventory, "tomeInventory");
                _statInv        = GameReflection.GetMember(inventory, "statInventory");
                _playerStats    = GameReflection.GetMember(inventory, "playerStats");

                if (tomeInv == null) { NotificationService.Show("Tome system not found.", null, NotificationService.NotificationType.Unlucky); return; }

                // Reuse FindTomeData via RandomTomeEffect (static helper not available; duplicate minimal logic)
                object tomeData = FindQuickTomeData(tomeInv);
                if (tomeData == null) { NotificationService.Show("Tome Lottery: No tome data found.", null, NotificationService.NotificationType.Unlucky); return; }

                _appliedStatModifier = GameReflection.GetMember(tomeData, "statModifier");
                if (_appliedStatModifier != null && _statInv != null)
                {
                    var t = _appliedStatModifier.GetType();
                    GameReflection.InvokeInstance(_statInv, "ChangeStat",
                        new[] { t, typeof(bool), typeof(float), typeof(bool) },
                        _appliedStatModifier, true, 0f, false);
                }

                try { _tomeName = GameReflection.GetMember(tomeData, "eTome")?.ToString(); } catch { }
                NotificationService.Show($"+Tome: {_tomeName ?? "???"} 📖", null, NotificationService.NotificationType.Reward);
            }
            catch (Exception ex) { Main.Error("[FakeTome] OnStart: " + ex.Message); }
        }

        private object FindQuickTomeData(object tomeInv)
        {
            try
            {
                var dict = GameReflection.GetMember(tomeInv, "tomeUpgrade") as System.Collections.IDictionary;
                if (dict != null && dict.Count > 0)
                {
                    var vals = new System.Collections.Generic.List<object>();
                    foreach (System.Collections.DictionaryEntry e in dict) if (e.Value != null) vals.Add(e.Value);
                    if (vals.Count > 0) return vals[new System.Random().Next(vals.Count)];
                }
            }
            catch { }
            
            try
            {
                var tomeDataType = GameReflection.FindType("TomeData");
                if (tomeDataType != null)
                {
                    var allObjs = GameReflection.FindObjectsOfTypeAll(tomeDataType);
                    if (allObjs != null)
                    {
                        var list = new System.Collections.Generic.List<object>();
                        foreach (var obj in allObjs) if (obj != null) list.Add(obj);
                        if (list.Count > 0) return list[new System.Random().Next(list.Count)];
                    }
                }
            }
            catch { }

            return null;
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }

        public void OnEnd()
        {
            try
            {
                if (_appliedStatModifier != null && _statInv != null)
                {
                    // Invert the stat modifier to undo the buff
                    var t = _appliedStatModifier.GetType();
                    // Negate by applying the same modifier but treat as removing
                    GameReflection.InvokeInstance(_statInv, "ChangeStat",
                        new[] { t, typeof(bool), typeof(float), typeof(bool) },
                        _appliedStatModifier, false, 0f, false);
                }
                if (_playerStats != null)
                    try { GameReflection.InvokeInstance(_playerStats, "ForceUpdateStats", Type.EmptyTypes); } catch { }

                NotificationService.Show("Tome Lottery was fake! Tome removed.", null, NotificationService.NotificationType.Warning);
                ChaosEngine.Instance.AddLogEntry("Tome Lottery (It was fake!)");
            }
            catch (Exception ex) { Main.Error("[FakeTome] OnEnd: " + ex.Message); }
        }
    }
}
