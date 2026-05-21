using System;
using System.Reflection;
using UnityEngine;

public class Test2 {
    public void Run() {
        var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Type) }, null);
        var res = findMethod.Invoke(null, new object[] { typeof(MonoBehaviour) });
    }
}
