using Harmony12;
using UnityEngine;

namespace DVDispatcherMod.HarmonyPatches {
    [HarmonyPatch(typeof(FloatieWithAnimation), "Start")]
    class FloatieWithAnimation_Start_Patch {
        static void Postfix(LineRenderer ___line) {
            ___line.startWidth = Constants.START_WIDTH;
            ___line.endWidth = Constants.END_WIDTH;
            ___line.material.mainTexture = PointerTexture.Texture;
        }
    }
}