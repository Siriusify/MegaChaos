using System;
using System.Reflection;
using UnityEngine;
using MegaChaos.Services;

namespace MegaChaos {
    public class TestLava {
        public static void Run() {
            var t = GameReflection.FindType("Il2CppAssets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers.ChallengeModifierLava", "Assets.Scripts._Data.Progression.Achievements.Challenges.ChallengeModifiers.ChallengeModifierLava", "ChallengeModifierLava");
            if(t != null) {
                foreach(var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    MegaChaos.Main.Msg($"Lava Field: {f.Name} - {f.FieldType}");
                }
                foreach(var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    MegaChaos.Main.Msg($"Lava Method: {m.Name}");
                }
            } else {
                MegaChaos.Main.Msg("Lava type not found");
            }
        }
    }
}
