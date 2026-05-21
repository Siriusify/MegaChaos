using System;
using System.Reflection;
using UnityEngine;
using MegaChaos.Services;

namespace MegaChaos {
    public class Test5 {
        public static void Run() {
            var t = GameReflection.FindType("StatModifier");
            if(t != null) {
                foreach(var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    MegaChaos.Main.Msg($"Field: {f.Name} - {f.FieldType}");
                }
            }
        }
    }
}
