using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UnityEngine;

namespace MegaChaos.Services;

internal static class ItemIconService
{
    private static readonly ConcurrentDictionary<string, IconResult> IconCache = new();
    private static readonly ConcurrentDictionary<string, float> MissingRetryAfter = new();
    private static readonly ConcurrentDictionary<string, byte> MissLogOnce = new();
    private static MethodInfo _findViaResources;
    private static MethodInfo _findViaObjectAll;
    private static MethodInfo _findViaObjectIncludingAssets;
    private static MethodInfo _findViaObjectTypeOnly;
    private static MethodInfo _findViaObjectTypeWithInactive;
    private static MethodInfo _findGenericResourcesAll;
    private static MethodInfo _findGenericObjectAll;
    private static MethodInfo _findGenericObjectType;
    private static MethodInfo _findGenericObjectByType;
    private static bool _findMethodsResolved;
    private static bool _finderProbeLogged;
    private static Type _itemDataType;
    private static Type _eItemType;
    private static bool _loggedObjectCounts;

    public static IconResult GetIcon(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        string key = itemName.ToLowerInvariant();
        
        if (key == "none" || key.StartsWith("chaos"))
        {
            if (IconCache.TryGetValue(key, out var noneCached))
                return noneCached;
                
            try 
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MegaChaos.nothing.raw");
                if (stream != null)
                {
                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    
                    var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    
                    for (int y = 0; y < 64; y++)
                    {
                        for (int x = 0; x < 64; x++)
                        {
                            int idx = (y * 64 + x) * 4;
                            tex.SetPixel(x, y, new Color32(bytes[idx], bytes[idx+1], bytes[idx+2], bytes[idx+3]));
                        }
                    }
                    tex.Apply();
                    
                    var res = new IconResult(tex, new Rect(0, 0, 1, 1), "nothing");
                    IconCache[key] = res;
                    return res;
                }
            } 
            catch (Exception ex) 
            {
                Main.Warn($"Failed to load nothing.raw: {ex.Message}");
            }
            return null;
        }

        if (IconCache.TryGetValue(key, out var cached))
            return cached;

        if (MissingRetryAfter.TryGetValue(key, out var retryAfter) && Time.unscaledTime < retryAfter)
            return null;

        try
        {
            var itemDataIcon = FindIconFromItemData(itemName, key);
            if (itemDataIcon != null)
            {
                IconCache[key] = itemDataIcon;
                MissingRetryAfter.TryRemove(key, out _);
                return itemDataIcon;
            }

            var spriteMatch = FindBestSprite(key);
            if (spriteMatch != null)
            {
                IconCache[key] = spriteMatch;
                MissingRetryAfter.TryRemove(key, out _);
                return spriteMatch;
            }

            var textureMatch = FindBestTexture(key);
            if (textureMatch != null)
            {
                IconCache[key] = textureMatch;
                MissingRetryAfter.TryRemove(key, out _);
                return textureMatch;
            }
        }
        catch (Exception ex)
        {
            Main.Warn($"Could not resolve icon for {itemName}: {ex.GetBaseException().Message}");
        }

        MissingRetryAfter[key] = Time.unscaledTime + 2f;
        WarnMissingOnce(itemName, key);
        return null;
    }

    private static IconResult FindIconFromItemData(string itemName, string key)
    {
        var itemValue = ResolveItemValue(itemName);

        _itemDataType ??= GameReflection.FindType("ItemData");
        if (_itemDataType == null)
            return null;

        var itemDataObjects = FindObjectsOfTypeAll(_itemDataType);
        if (itemDataObjects == null)
            return null;

        LogObjectCounts(CountEnumerable(itemDataObjects));

        object bestItemData = null;
        var bestScore = 0;

        foreach (var itemData in itemDataObjects)
        {
            if (itemData == null)
                continue;

            var score = ScoreItemDataMatch(itemData, itemValue, key);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestItemData = itemData;
        }

        if (bestItemData == null)
            return null;

        var icon = TryGetIconFromItemData(bestItemData);
        if (icon != null)
            return icon;

        return icon;
    }

    private static IconResult TryGetIconFromItemData(object itemData)
    {
        var icon = ToIconResult(GameReflection.InvokeInstance(itemData, "GetIcon", Type.EmptyTypes))
            ?? ToIconResult(GameReflection.GetMember(itemData, "icon"));

        if (icon != null)
            return icon;

        var dummyItem = GameReflection.InvokeInstance(itemData, "GetDummyItem", Type.EmptyTypes)
            ?? GameReflection.GetMember(itemData, "dummyItem");

        return ToIconResult(GameReflection.InvokeInstance(dummyItem, "GetIcon", Type.EmptyTypes))
            ?? ToIconResult(GameReflection.GetMember(dummyItem, "icon"));
    }

    private static IconResult ToIconResult(object value)
    {
        if (value is Sprite sprite && sprite.texture != null)
        {
            var spriteTexture = sprite.texture;
            if (spriteTexture.width <= 0 || spriteTexture.height <= 0)
                return null;

            var textureRect = sprite.textureRect;
            if (textureRect.width <= 0f || textureRect.height <= 0f)
                return null;

            var uv = new Rect(
                textureRect.x / spriteTexture.width,
                textureRect.y / spriteTexture.height,
                textureRect.width / spriteTexture.width,
                textureRect.height / spriteTexture.height);

            if (!IsFiniteRect(uv))
                return null;

            return new IconResult(spriteTexture, uv, sprite.name);
        }

        return value is Texture fullTexture
            ? new IconResult(fullTexture, new Rect(0f, 0f, 1f, 1f), fullTexture.name)
            : null;
    }

    private static object ResolveItemValue(string itemName)
    {
        var cacheKey = RewardRule.Normalize(itemName);
        if (string.IsNullOrWhiteSpace(cacheKey))
            return null;

        _eItemType ??= GameReflection.FindType(
            "Il2CppAssets.Scripts.Inventory__Items__Pickups.Items.EItem",
            "Assets.Scripts.Inventory__Items__Pickups.Items.EItem",
            "EItem");

        if (_eItemType == null)
            return null;

        foreach (var enumName in Enum.GetNames(_eItemType))
        {
            if (RewardRule.Normalize(enumName) == cacheKey)
                return Enum.Parse(_eItemType, enumName);
        }

        return null;
    }

    private static bool IsSameItem(object candidate, object expected, string expectedKey)
    {
        if (candidate == null || expected == null)
            return false;

        if (candidate.Equals(expected))
            return true;

        return NormalizeForIconLookup(candidate.ToString()) == expectedKey;
    }

    private static int ScoreItemDataMatch(object itemData, object itemValue, string key)
    {
        var score = 0;
        var eItem = GameReflection.GetMember(itemData, "eItem");
        if (IsSameItem(eItem, itemValue, key))
            score = Math.Max(score, 10000);

        score = Math.Max(score, ScoreNameMatch(key, itemData.GetType().Name));
        score = Math.Max(score, ScoreNameMatch(key, itemData.GetType().FullName));

        var dummyItem = GameReflection.GetMember(itemData, "dummyItem");
        if (dummyItem != null)
        {
            score = Math.Max(score, ScoreNameMatch(key, dummyItem.GetType().Name));
            score = Math.Max(score, ScoreNameMatch(key, dummyItem.GetType().FullName));
        }

        if (eItem != null)
            score = Math.Max(score, ScoreNameMatch(key, eItem.ToString()));

        return score;
    }

    private static IconResult FindBestSprite(string key)
    {
        var objects = FindObjectsOfTypeAll(typeof(Sprite));
        if (objects == null)
            return null;

        Sprite bestSprite = null;
        var bestScore = 0;

        foreach (var obj in objects)
        {
            var sprite = obj as Sprite;
            if (sprite == null || sprite.texture == null || string.IsNullOrWhiteSpace(sprite.name))
                continue;

            var score = ScoreNameMatch(key, sprite.name);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestSprite = sprite;
        }

        if (bestSprite == null)
            return null;

        var texture = bestSprite.texture;
        var textureRect = bestSprite.textureRect;
        var uv = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        if (!IsFiniteRect(uv))
            return null;

        return new IconResult(texture, uv, bestSprite.name);
    }

    private static IconResult FindBestTexture(string key)
    {
        var objects = FindObjectsOfTypeAll(typeof(Texture2D));
        if (objects == null)
            return null;

        Texture2D bestTexture = null;
        var bestScore = 0;

        foreach (var obj in objects)
        {
            var texture = obj as Texture2D;
            if (texture == null || string.IsNullOrWhiteSpace(texture.name))
                continue;

            var score = ScoreNameMatch(key, texture.name);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestTexture = texture;
        }

        return bestTexture == null
            ? null
            : LogTextureFallback(bestTexture);
    }

    private static IconResult LogTextureFallback(Texture2D texture)
    {
        return new IconResult(texture, new Rect(0f, 0f, 1f, 1f), texture.name);
    }

    private static int ScoreNameMatch(string key, string candidateName)
    {
        var candidate = NormalizeForIconLookup(candidateName);
        if (string.IsNullOrEmpty(candidate))
            return 0;

        if (candidate == key)
            return 10000;

        var hasIconHint = candidate.Contains("icon") || candidate.Contains("sprite") || candidate.Contains("item");
        if (candidate.EndsWith(key, StringComparison.Ordinal))
            return hasIconHint ? 9000 : 7000;

        if (candidate.StartsWith(key, StringComparison.Ordinal))
            return hasIconHint ? 8500 : 6500;

        if (candidate.Contains(key, StringComparison.Ordinal))
        {
            var lengthPenalty = Math.Min(2000, candidate.Length - key.Length);
            return (hasIconHint ? 8000 : 6000) - lengthPenalty;
        }

        return 0;
    }

    private static string NormalizeForIconLookup(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new List<char>(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
                chars.Add(char.ToLowerInvariant(character));
        }

        return new string(chars.ToArray());
    }

    private static IEnumerable FindObjectsOfTypeAll(Type type)
    {
        if (type == null)
            return null;

        try
        {
            EnsureFindMethodsResolved();
            LogFinderProbeOnce();

            // First, try generic finder APIs discovered via reflection. Some Unity builds expose
            // only generic variants and strip type-based overloads.
            var genericObjects = InvokeGenericFinder(_findGenericResourcesAll, type)
                ?? InvokeGenericFinder(_findGenericObjectAll, type)
                ?? InvokeGenericFinder(_findGenericObjectType, type)
                ?? InvokeGenericFinder(_findGenericObjectByType, type);
            if (genericObjects != null)
                return genericObjects;

            if (_itemDataType != null && type == _itemDataType)
            {
                var fromScriptables = FindItemDataViaScriptableObjects(type);
                if (fromScriptables != null)
                    return fromScriptables;
            }

            var objects = InvokeFinder(_findViaResources, type);
            if (objects != null)
                return objects;

            objects = InvokeFinder(_findViaObjectAll, type);
            if (objects != null)
                return objects;

            objects = InvokeFinder(_findViaObjectIncludingAssets, type);
            if (objects != null)
                return objects;

            objects = InvokeFinder(_findViaObjectTypeWithInactive, type, false);
            if (objects != null)
                return objects;

            return InvokeFinder(_findViaObjectTypeOnly, type);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable FindItemDataViaScriptableObjects(Type itemDataType)
    {
        // Avoid generic API usage because some game Unity builds strip it.
        var scriptableType = typeof(ScriptableObject);
        var allScriptables = InvokeFinder(_findViaResources, scriptableType)
            ?? InvokeFinder(_findViaObjectAll, scriptableType)
            ?? InvokeFinder(_findViaObjectIncludingAssets, scriptableType)
            ?? InvokeFinder(_findViaObjectTypeWithInactive, scriptableType, false)
            ?? InvokeFinder(_findViaObjectTypeOnly, scriptableType);

        if (allScriptables == null)
            return null;

        var matches = new List<object>();
        var enumerator = allScriptables.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var entry = enumerator.Current;
            if (entry == null)
                continue;

            var entryType = entry.GetType();
            if (entryType == itemDataType || entryType.FullName == itemDataType.FullName || entryType.Name == itemDataType.Name)
                matches.Add(entry);
        }

        return matches.Count == 0 ? null : matches;
    }

    private static void EnsureFindMethodsResolved()
    {
        if (_findMethodsResolved)
            return;

        _findMethodsResolved = true;

        const BindingFlags staticPublic = BindingFlags.Public | BindingFlags.Static;

        _findViaResources = typeof(Resources).GetMethod("FindObjectsOfTypeAll", staticPublic, null, new[] { typeof(Type) }, null);
        _findViaObjectAll = typeof(UnityEngine.Object).GetMethod("FindObjectsOfTypeAll", staticPublic, null, new[] { typeof(Type) }, null);
        _findViaObjectIncludingAssets = typeof(UnityEngine.Object).GetMethod("FindObjectsOfTypeIncludingAssets", staticPublic, null, new[] { typeof(Type) }, null);
        _findViaObjectTypeOnly = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", staticPublic, null, new[] { typeof(Type) }, null);
        _findViaObjectTypeWithInactive = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", staticPublic, null, new[] { typeof(Type), typeof(bool) }, null);

        _findGenericResourcesAll = FindGenericFinder(typeof(Resources), "FindObjectsOfTypeAll");
        _findGenericObjectAll = FindGenericFinder(typeof(UnityEngine.Object), "FindObjectsOfTypeAll");
        _findGenericObjectType = FindGenericFinder(typeof(UnityEngine.Object), "FindObjectsOfType");
        _findGenericObjectByType = FindGenericFinder(typeof(UnityEngine.Object), "FindObjectsByType");
    }

    private static IEnumerable InvokeFinder(MethodInfo method, params object[] args)
    {
        if (method == null)
            return null;

        try
        {
            var result = method.Invoke(null, args);
            return result as IEnumerable;
        }
        catch
        {
            return null;
        }
    }

    private static MethodInfo FindGenericFinder(Type ownerType, string methodName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        var methods = ownerType.GetMethods(flags);
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (method.Name != methodName || !method.IsGenericMethodDefinition)
                continue;

            if (method.GetGenericArguments().Length != 1)
                continue;

            return method;
        }

        return null;
    }

    private static IEnumerable InvokeGenericFinder(MethodInfo genericMethod, Type targetType)
    {
        if (genericMethod == null || targetType == null)
            return null;

        try
        {
            var closedMethod = genericMethod.MakeGenericMethod(targetType);
            var parameters = closedMethod.GetParameters();
            var args = BuildDefaultArgs(parameters);
            var result = closedMethod.Invoke(null, args);
            return result as IEnumerable;
        }
        catch
        {
            return null;
        }
    }

    private static object[] BuildDefaultArgs(ParameterInfo[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return Array.Empty<object>();

        var args = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var pType = parameters[i].ParameterType;
            if (pType == typeof(bool))
            {
                args[i] = false;
                continue;
            }

            if (pType.IsEnum)
            {
                var values = Enum.GetValues(pType);
                args[i] = values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(pType);
                continue;
            }

            args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : null;
        }

        return args;
    }

    private static void LogFinderProbeOnce()
    {
    }

    private static void LogObjectCounts(int itemDataCount)
    {
    }

    private static void WarnMissingOnce(string itemName, string key)
    {
    }

    private static int CountEnumerable(IEnumerable enumerable)
    {
        if (enumerable == null)
            return 0;

        var count = 0;
        var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext())
            count++;

        return count;
    }

    private static bool IsFiniteRect(Rect rect)
    {
        return IsFinite(rect.x) && IsFinite(rect.y) && IsFinite(rect.width) && IsFinite(rect.height);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal sealed class IconResult
    {
        public IconResult(Texture texture, Rect uv, string sourceName)
        {
            Texture = texture;
            Uv = uv;
            SourceName = sourceName;
        }

        public Texture Texture { get; }

        public Rect Uv { get; }

        public string SourceName { get; }
    }

}
