using Harmony12;
using UnityEngine;

namespace DVDispatcherMod {
    public static class Constants {
        public const float START_WIDTH = 0.0025f; // Original = 0.005f;
        public const float END_WIDTH = 0.01f; // Original = 0.02f;
    }

    [HarmonyPatch(typeof(FloatieWithAnimation), "Start")]
    class FloatieWithAnimation_Start_Patch {
        static void Postfix(LineRenderer ___line) {
            ___line.startWidth = Constants.START_WIDTH;
            ___line.endWidth = Constants.END_WIDTH;
            ___line.material.mainTexture = PointerTexture.Texture;
        }
    }

    [HarmonyPatch(typeof(TutorialLineNonVR), "Start")]
    class TutorialLineNonVR_Start_Patch {
        static void Postfix(LineRenderer ___line) {
            ___line.startWidth = Constants.START_WIDTH;
            ___line.endWidth = Constants.END_WIDTH;
            ___line.material.mainTexture = PointerTexture.Texture;
        }
    }
}