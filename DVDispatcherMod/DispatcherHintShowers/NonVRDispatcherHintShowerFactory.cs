using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DVDispatcherMod.DispatcherHintShowers {
    public static class NonVRDispatcherHintShowerFactory {
        private static bool _floatieSceneLoaded;

        public static NonVRDispatcherHintShower TryCreate() {
            var g = GameObject.Find("[NonVRFloatie]");
            if (g == null) {
                if (!_floatieSceneLoaded) {
                    SceneManager.LoadScene("non_vr_ui_floatie", LoadSceneMode.Additive);
                    _floatieSceneLoaded = true;
                    Main.ModEntry.Logger.Log("Called load of non VR float scene.");
                } else {
                    Main.ModEntry.Logger.Log("Could not find the non VR float.");
                }
                return null;
            }

            Main.ModEntry.Logger.Log("Found the GameObject [NonVRFloatie].");
            
            g = Object.Instantiate(g); // The tutorial sequence destroys non VR floaties, so make our own.
            Main.ModEntry.Logger.Log("Duplicated the GameObject [NonVRFloatie].");

            var floatieNonVr = g.GetComponentInChildren<Image>(true)?.gameObject;
            if (floatieNonVr == null) {
                Main.ModEntry.Logger.Log("Could not find the floatieNonVr.");
                return null;
            }
            Main.ModEntry.Logger.Log("Found the floatieNonVr.");

            var floatieNonVrText = floatieNonVr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (floatieNonVrText == null) {
                Main.ModEntry.Logger.Log("Could not find the floatieNonVrText.");
                return null;
            }
            Main.ModEntry.Logger.Log("Found the floatieNonVrText.");

            var floatieNonVrLine = floatieNonVr.GetComponentInChildren<NonVRLineRendererController>(true);
            if (floatieNonVrLine == null) {
                Main.ModEntry.Logger.Log("Could not find the floatieNonVrLine.");
                return null;
            }
            Main.ModEntry.Logger.Log("Found the floatieNonVrLine.");
            return new NonVRDispatcherHintShower(floatieNonVr, floatieNonVrText, floatieNonVrLine);
        }
    }
}