using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class RandomTomeEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_randomtome";
        public string Name => _displayName;
        public string Description => "Grants a random permanent tome effect!";
        public float DefaultDuration => 0f;

        private string _displayName = "Tome Lottery";

        public bool HideProgressBar => true;
        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        private static readonly System.Random _rng = new();

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory = GameReflection.GetMember(player, "inventory");
                var tomeInv = GameReflection.GetMember(inventory, "tomeInventory");

                if (tomeInv == null)
                {
                    NotificationService.Show("Tome system not found.", null, NotificationService.NotificationType.Unlucky);
                    return;
                }

                var tomeData = FindTomeData(tomeInv);

                if (tomeData == null)
                {
                    NotificationService.Show("Tome Lottery: Failed to load tome data.", null, NotificationService.NotificationType.Unlucky);
                    MegaChaos.Main.Warn("[RandomTome] TomeData bulunamadı.");
                    return;
                }

                var eRarityType = GameReflection.FindType("Il2CppAssets.Scripts.Inventory__Items__Pickups.ERarity", "Assets.Scripts.Inventory__Items__Pickups.ERarity", "ERarity");
                object rarity = 0; // ERarity.New is usually 0
                if (eRarityType != null)
                {
                    try { rarity = Enum.Parse(eRarityType, "New"); } catch { }
                }

                object upgradeOffer = null;
                try { upgradeOffer = GameReflection.InvokeInstance(tomeData, "GetUpgradeOffer", new[] { eRarityType ?? typeof(int) }, rarity); } catch { }

                var addTomeMethod = GameReflection.FindAnyMethod(tomeInv.GetType(), "AddTome");
                if (addTomeMethod != null)
                {
                    var prms = addTomeMethod.GetParameters();
                    if (prms.Length == 3)
                        addTomeMethod.Invoke(tomeInv, new object[] { tomeData, upgradeOffer, rarity });
                    else if (prms.Length == 2)
                        addTomeMethod.Invoke(tomeInv, new object[] { tomeData, rarity });
                    else if (prms.Length == 1)
                        addTomeMethod.Invoke(tomeInv, new object[] { tomeData });
                }

                var playerStats = GameReflection.GetMember(inventory, "playerStats");
                if (playerStats != null)
                {
                    GameReflection.InvokeInstance(playerStats, "ForceUpdateStats", Type.EmptyTypes);
                }

                object tomeNameObj = null;
                try { tomeNameObj = GameReflection.GetMember(tomeData, "eTome"); } catch { }
                if (tomeNameObj == null) try { tomeNameObj = GameReflection.InvokeInstance(tomeData, "GetName", Type.EmptyTypes); } catch { }

                string tNameStr = tomeNameObj?.ToString() ?? "???";
                if (tNameStr.StartsWith("Tome")) tNameStr = tNameStr.Substring(4);
                tNameStr = System.Text.RegularExpressions.Regex.Replace(tNameStr, "([a-z])([A-Z])", "$1 $2");

                _displayName = $"Tome Lottery: {tNameStr}";

                NotificationService.Show($"+Tome: {tNameStr} 📖", null, NotificationService.NotificationType.Reward);
                MegaChaos.Main.Msg($"[RandomTome] Tome eklendi: {tNameStr}");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[RandomTome] " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        private object FindTomeData(object tomeInv)
        {
            try
            {
                var alwaysManagerType = GameReflection.FindType("AlwaysManager", "Il2CppAlwaysManager");
                var amInstance = GameReflection.GetStaticMember(alwaysManagerType, "Instance");
                var dataManager = GameReflection.GetMember(amInstance, "dataManager");
                var allTomes = GameReflection.GetMember(dataManager, "tomeData");

                if (allTomes == null) return null;

                var unowned = new List<object>();
                var fallback = new List<object>();

                var eTomeType = GameReflection.FindType("Il2CppAssets.Scripts._Data.Tomes.ETome", "Assets.Scripts._Data.Tomes.ETome", "ETome");

                var enumerator = GameReflection.InvokeInstance(allTomes, "GetEnumerator", Type.EmptyTypes);
                if (enumerator != null)
                {
                    var moveNextMethod = GameReflection.FindAnyMethod(enumerator.GetType(), "MoveNext");
                    var currentProp = enumerator.GetType().GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                    if (moveNextMethod != null && currentProp != null)
                    {
                        while ((bool)moveNextMethod.Invoke(enumerator, null))
                        {
                            var currentKV = currentProp.GetValue(enumerator);
                            var eTome = GameReflection.GetMember(currentKV, "Key");
                            var td = GameReflection.GetMember(currentKV, "Value");

                            if (td == null) continue;

                            fallback.Add(td);
                            try
                            {
                                var has = GameReflection.InvokeInstance(tomeInv, "HasTome", new[] { eTomeType }, eTome);
                                if (has == null || !(bool)has) unowned.Add(td);
                            }
                            catch { unowned.Add(td); }
                        }
                    }
                }

                var candidates = unowned.Count > 0 ? unowned : fallback;
                if (candidates.Count > 0)
                {
                    return candidates[_rng.Next(candidates.Count)];
                }
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Warn("[RandomTome] FindTomeData error: " + ex.Message);
            }

            return null;
        }

        public void OnUpdate(float dt) { }
        public void OnGUI() { }
        public void OnEnd() { }
    }

    public class FakeTomeLotteryEffect : IChaosEffect, IChaosOverlayEffect
    {
        public string Id => "effect_faketome";
        public string Name => _displayName;
        public string Description => "Gives a tome… then takes it back after 5 seconds!";
        public float DefaultDuration => 5f;

        private string _displayName = "Tome Lottery";
        public bool HideProgressBar => true;
        public float? GetProgress01(float remainingTime, float totalDuration) => null;

        private object _addedETome;
        private string _tomeName;
        private object _tomeInv;
        private object _playerStats;

        public void OnStart()
        {
            try
            {
                var myPlayerType = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Player.MyPlayer", "Assets.Scripts.Actors.Player.MyPlayer", "MyPlayer");
                var player = GameReflection.GetStaticMember(myPlayerType, "Instance");
                var inventory = GameReflection.GetMember(player, "inventory");
                _tomeInv = GameReflection.GetMember(inventory, "tomeInventory");
                _playerStats = GameReflection.GetMember(inventory, "playerStats");

                if (_tomeInv == null)
                {
                    NotificationService.Show("Tome system not found.", null, NotificationService.NotificationType.Unlucky);
                    return;
                }

                var tomeData = FindUnownedTomeData(_tomeInv);
                if (tomeData == null)
                {
                    NotificationService.Show("Tome Lottery: No new tomes available.", null, NotificationService.NotificationType.Unlucky);
                    return;
                }

                object tomeNameObj = null;
                try { tomeNameObj = GameReflection.GetMember(tomeData, "eTome"); } catch { }
                if (tomeNameObj == null) try { tomeNameObj = GameReflection.InvokeInstance(tomeData, "GetName", Type.EmptyTypes); } catch { }

                _addedETome = tomeNameObj;

                var eRarityType = GameReflection.FindType("Il2CppAssets.Scripts.Inventory__Items__Pickups.ERarity", "Assets.Scripts.Inventory__Items__Pickups.ERarity", "ERarity");
                object rarity = 0; // ERarity.New
                if (eRarityType != null) { try { rarity = Enum.Parse(eRarityType, "New"); } catch { } }

                object upgradeOffer = null;
                try { upgradeOffer = GameReflection.InvokeInstance(tomeData, "GetUpgradeOffer", new[] { eRarityType ?? typeof(int) }, rarity); } catch { }

                var addTomeMethod = GameReflection.FindAnyMethod(_tomeInv.GetType(), "AddTome");
                if (addTomeMethod != null)
                {
                    var prms = addTomeMethod.GetParameters();
                    if (prms.Length == 3)
                        addTomeMethod.Invoke(_tomeInv, new object[] { tomeData, upgradeOffer, rarity });
                    else if (prms.Length == 2)
                        addTomeMethod.Invoke(_tomeInv, new object[] { tomeData, rarity });
                    else if (prms.Length == 1)
                        addTomeMethod.Invoke(_tomeInv, new object[] { tomeData });
                }

                if (_playerStats != null)
                {
                    GameReflection.InvokeInstance(_playerStats, "ForceUpdateStats", Type.EmptyTypes);
                }

                string tNameStr = tomeNameObj?.ToString() ?? "???";
                if (tNameStr.StartsWith("Tome")) tNameStr = tNameStr.Substring(4);
                tNameStr = System.Text.RegularExpressions.Regex.Replace(tNameStr, "([a-z])([A-Z])", "$1 $2");
                _tomeName = tNameStr;

                _displayName = $"Tome Lottery: {tNameStr}";
                NotificationService.Show($"+Tome: {tNameStr} 📖", null, NotificationService.NotificationType.Reward);
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[FakeTome] OnStart: " + ex.Message);
            }
        }

        private object FindUnownedTomeData(object tomeInv)
        {
            try
            {
                var alwaysManagerType = GameReflection.FindType("AlwaysManager", "Il2CppAlwaysManager");
                var amInstance = GameReflection.GetStaticMember(alwaysManagerType, "Instance");
                var dataManager = GameReflection.GetMember(amInstance, "dataManager");
                var allTomes = GameReflection.GetMember(dataManager, "tomeData");

                if (allTomes == null) return null;

                var unowned = new List<object>();
                var eTomeType = GameReflection.FindType("Il2CppAssets.Scripts._Data.Tomes.ETome", "Assets.Scripts._Data.Tomes.ETome", "ETome");

                var enumerator = GameReflection.InvokeInstance(allTomes, "GetEnumerator", Type.EmptyTypes);
                if (enumerator != null)
                {
                    var moveNextMethod = GameReflection.FindAnyMethod(enumerator.GetType(), "MoveNext");
                    var currentProp = enumerator.GetType().GetProperty("Current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                    if (moveNextMethod != null && currentProp != null)
                    {
                        while ((bool)moveNextMethod.Invoke(enumerator, null))
                        {
                            var currentKV = currentProp.GetValue(enumerator);
                            var eTome = GameReflection.GetMember(currentKV, "Key");
                            var td = GameReflection.GetMember(currentKV, "Value");

                            if (td == null) continue;

                            try
                            {
                                var has = GameReflection.InvokeInstance(tomeInv, "HasTome", new[] { eTomeType }, eTome);
                                if (has == null || !(bool)has) unowned.Add(td);
                            }
                            catch { unowned.Add(td); }
                        }
                    }
                }

                if (unowned.Count > 0)
                {
                    return unowned[new System.Random().Next(unowned.Count)];
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
                if (_tomeInv != null && _addedETome != null)
                {
                    var eTomeType = GameReflection.FindType("Il2CppAssets.Scripts._Data.Tomes.ETome", "Assets.Scripts._Data.Tomes.ETome", "ETome");
                    if (eTomeType != null)
                    {
                        var upgrades = GameReflection.GetMember(_tomeInv, "tomeUpgrade");
                        var levels = GameReflection.GetMember(_tomeInv, "tomeLevels");

                        if (upgrades != null) GameReflection.InvokeInstance(upgrades, "Remove", new[] { eTomeType }, _addedETome);
                        if (levels != null) GameReflection.InvokeInstance(levels, "Remove", new[] { eTomeType }, _addedETome);

                        var uiManagerType = GameReflection.FindType("UiManager", "Il2CppUiManager");
                        if (uiManagerType != null)
                        {
                            var uiInstance = GameReflection.GetStaticMember(uiManagerType, "Instance");
                            if (uiInstance != null) GameReflection.InvokeInstance(uiInstance, "RefreshUi", Type.EmptyTypes);
                        }
                    }
                }

                if (_playerStats != null)
                {
                    GameReflection.InvokeInstance(_playerStats, "ForceUpdateStats", Type.EmptyTypes);
                }

                NotificationService.Show("Tome Lottery was fake! Tome removed.", null, NotificationService.NotificationType.Warning);
                ChaosEngine.Instance.AddLogEntry("Tome Lottery (It was fake!)");
            }
            catch (Exception ex)
            {
                MegaChaos.Main.Error("[FakeTome] OnEnd: " + ex.Message);
            }
        }
    }
}
