using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace MegaChaos.Services;

internal static class GameReflection
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();
    private static readonly ConcurrentDictionary<string, MemberInfo> MemberCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    public static Type FindType(params string[] typeNames)
    {
        Type resolvedType = null;

        foreach (var typeName in typeNames)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                continue;

            if (TypeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            var type = FindLoadedType(typeName);
            if (type != null)
            {
                resolvedType = type;
                break;
            }
        }

        if (resolvedType == null)
            return null;

        foreach (var typeName in typeNames)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                continue;

            TypeCache[typeName] = resolvedType;
        }

        return resolvedType;
    }

    public static object GetStaticMember(Type type, string name)
    {
        if (type == null)
            return null;

        return GetMemberValue(type, null, name);
    }

        public static object GetMember(object instance, string name)
        {
            if (instance == null)
                return null;

            return GetMemberValue(instance.GetType(), instance, name);
        }

        public static void SetMember(object instance, string name, object value)
        {
            if (instance == null) return;
            var type = instance.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase;
            
            var property = type.GetProperty(name, flags);
            if (property != null)
            {
                property.SetValue(instance, value);
                return;
            }
            
            var field = type.GetField(name, flags);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }

    public static MethodInfo FindAnyMethod(Type type, string methodName)
    {
        if (type == null)
            return null;

        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var method in methods)
        {
            if (method.Name == methodName)
                return method;
        }

        return null;
    }

    public static object InvokeStatic(Type type, string methodName, Type[] parameterTypes, params object[] args)
    {
        if (type == null)
            return null;

        var method = GetMethodInfo(type, methodName, parameterTypes, true);
        return method?.Invoke(null, args);
    }

    public static object InvokeInstance(object instance, string methodName, Type[] parameterTypes, params object[] args)
    {
        if (instance == null)
            return null;

        var method = GetMethodInfo(instance.GetType(), methodName, parameterTypes, false);
        return method?.Invoke(instance, args);
    }

    private static Type FindLoadedType(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType(typeName, false);
            if (type != null)
                return type;
        }

        foreach (var assembly in assemblies)
        {
            if (!ShouldEnumerateTypes(assembly))
                continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.FullName == typeName || type.Name == typeName)
                    return type;
            }
        }

        return null;
    }

    private static bool ShouldEnumerateTypes(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (string.IsNullOrEmpty(name))
            return false;

        return name.Contains("Assembly-CSharp")
            || name.StartsWith("Il2CppAssets", StringComparison.Ordinal)
            || name == "Il2Cpp";
    }

    private static object GetMemberValue(Type type, object instance, string name)
    {
        var member = GetMemberInfo(type, name);
        return member switch
        {
            PropertyInfo property => property.GetValue(instance),
            FieldInfo field => field.GetValue(instance),
            MethodInfo method when method.GetParameters().Length == 0 => method.Invoke(method.IsStatic ? null : instance, null),
            _ => null
        };
    }

    private static MemberInfo GetMemberInfo(Type type, string name)
    {
        var key = $"{type.AssemblyQualifiedName}:{name}";
        if (MemberCache.TryGetValue(key, out var cachedMember))
            return cachedMember;

        var member = FindMember(type, name);
        if (member == null)
            member = FindMemberCaseInsensitive(type, name);
        if (member == null)
            member = FindGetterMember(type, name, false);
        if (member == null)
            member = FindGetterMember(type, name, true);

        if (member != null)
            MemberCache[key] = member;

        return member;
    }

    private static MemberInfo FindMember(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        var property = type.GetProperty(name, flags);
        if (property != null)
            return property;

        return type.GetField(name, flags);
    }

    private static MemberInfo FindMemberCaseInsensitive(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase;

        var property = type.GetProperty(name, flags);
        if (property != null)
            return property;

        return type.GetField(name, flags);
    }

    private static MemberInfo FindGetterMember(Type type, string name, bool ignoreCase)
    {
        var flags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;

        if (ignoreCase)
            flags |= BindingFlags.IgnoreCase;

        var getter = type.GetMethod($"get_{name}", flags);
        return getter != null && getter.GetParameters().Length == 0 ? getter : null;
    }

    private static MethodInfo GetMethodInfo(Type type, string methodName, Type[] parameterTypes, bool isStatic)
    {
        var key = $"{type.AssemblyQualifiedName}:{methodName}:{isStatic}:{GetParameterCacheKey(parameterTypes)}";
        if (MethodCache.TryGetValue(key, out var cachedMethod))
            return cachedMethod;

        var method = FindMethod(type, methodName, parameterTypes, isStatic);
        if (method != null)
            MethodCache[key] = method;

        return method;
    }

    private static MethodInfo FindMethod(Type type, string methodName, Type[] parameterTypes, bool isStatic)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        var methods = type.GetMethods(flags);
        MethodInfo fallback = null;

        foreach (var method in methods)
        {
            if (method.Name != methodName)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != (parameterTypes?.Length ?? 0))
                continue;

            if (IsExactParameterMatch(parameters, parameterTypes))
                return method;

            if (fallback == null && IsCompatibleParameterMatch(parameters, parameterTypes))
                fallback = method;
        }

        return fallback;
    }

    private static bool IsExactParameterMatch(ParameterInfo[] parameters, Type[] parameterTypes)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameterTypes[i] == null || parameters[i].ParameterType != parameterTypes[i])
                return false;
        }

        return true;
    }

    private static bool IsCompatibleParameterMatch(ParameterInfo[] parameters, Type[] parameterTypes)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameterTypes[i] == null)
                continue;

            if (!parameters[i].ParameterType.IsAssignableFrom(parameterTypes[i]))
                return false;
        }

        return true;
    }

    private static string GetParameterCacheKey(Type[] parameterTypes)
    {
        if (parameterTypes == null || parameterTypes.Length == 0)
            return string.Empty;

        var parts = new string[parameterTypes.Length];
        for (var i = 0; i < parameterTypes.Length; i++)
            parts[i] = parameterTypes[i]?.AssemblyQualifiedName ?? "<null>";

        return string.Join(",", parts);
    }
}
