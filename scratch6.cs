using System;
using System.Reflection;
using UnityEngine;
using MegaChaos.Services;

namespace MegaChaos {
    public class Test6 {
        public static void Run() {
            var t = GameReflection.FindType("PlayerHealth");
            if(t != null) {
                foreach(var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    MegaChaos.Main.Msg($"PlayerHealth Field: {f.Name} - {f.FieldType}");
                }
            }
            var e = GameReflection.FindType("Il2CppAssets.Scripts.Actors.Enemies.Enemy", "Assets.Scripts.Actors.Enemies.Enemy", "Enemy");
            if(e != null) {
                foreach(var f in e.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    MegaChaos.Main.Msg($"Enemy Field: {f.Name} - {f.FieldType}");
                }
            }
        }
    }
}
