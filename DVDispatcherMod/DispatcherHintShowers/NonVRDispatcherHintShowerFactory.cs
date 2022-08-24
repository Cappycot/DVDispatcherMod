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
            g = Object.Instantiate(g); // The tutorial sequence destroys non VR floaties, so make our own.

            var floatieNonVr = g.GetComponentInChildren<Image>(true)?.gameObject;
            if (floatieNonVr == null) {
                return null;
            }
            Main.ModEntry.Logger.Log("Found the non VR float.");

            var floatieNonVrText = floatieNonVr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (floatieNonVrText == null) {
                return null;
            }
            Main.ModEntry.Logger.Log("Found the non VR text.");

            var floatieNonVrLine = floatieNonVr.GetComponentInChildren<TutorialLineNonVR>(true);
            if (floatieNonVrLine == null) {
                return null;
            }
            Main.ModEntry.Logger.Log("Found the non VR line.");
            return new NonVRDispatcherHintShower(floatieNonVr, floatieNonVrText, floatieNonVrLine);
        }
    }
}