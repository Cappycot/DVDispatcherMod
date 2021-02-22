using Harmony12;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRTK;

namespace DVDispatcherMod
{
    public static class Floaties
    {

        // NonVR Floating Text
        public static GameObject floatieNonVR;
        private static TutorialLineNonVR floatieNonVRLine;
        private static TextMeshProUGUI floatieNonVRText;
        private static bool floatieSceneLoaded = false;

        // VR Floating Text
        private static GameObject floatieVR;

        // Floating Pointer
        public const float START_WIDTH = 0.0025f; // Original = 0.005f;
        public const float END_WIDTH = 0.01f; // Original = 0.02f;
        public static Texture2D pointerTexture;

        // Hide and show floating text.
        public delegate void ChangeFloatieTextDelegate(string text);
        public static ChangeFloatieTextDelegate ChangeFloatieText;
        public delegate void HideFloatieDelegate();
        public static HideFloatieDelegate HideFloatie;
        public delegate void ShowFloatieDelegate(string text);
        public static ShowFloatieDelegate ShowFloatie;
        public delegate void UpdateAttentionTransformDelegate(Transform attentionTransform);
        public static UpdateAttentionTransformDelegate UpdateAttentionTransform;

        public static void Initialize()
        {
            pointerTexture = new Texture2D(256, 1);
            // Note: ImageConversion.LoadImage automatically invokes Apply.
            ImageConversion.LoadImage(pointerTexture, File.ReadAllBytes(Main.mod.Path + "tutorial_UI_gradient_opaque.png"));
            // Solely based on command line args, so fine to init ASAP.
            if (VRManager.IsVREnabled())
            {
                ChangeFloatieText = ChangeFloatieTextVR;
                HideFloatie = HideFloatVR;
                ShowFloatie = ShowFloatVR;
                UpdateAttentionTransform = UpdateAttentionTransformVR;
            }
            else
            {
                ChangeFloatieText = ChangeFloatieTextNonVR;
                HideFloatie = HideFloatNonVR;
                ShowFloatie = ShowFloatNonVR;
                UpdateAttentionTransform = UpdateAttentionTransformNonVR;
            }
        }

        public static bool InitFloatieNonVR()
        {
            GameObject g = GameObject.Find("[NonVRFloatie]");
            if (g == null)
            {
                if (!floatieSceneLoaded)
                {
                    SceneManager.LoadScene("non_vr_ui_floatie", LoadSceneMode.Additive);
                    floatieSceneLoaded = true;
                    Main.mod.Logger.Log("Called load of non VR float scene.");
                }
                else
                    Main.mod.Logger.Log("Could not find the non VR float.");
                return false;
            }
            g = GameObject.Instantiate(g); // The tutorial sequence destroys non VR floaties, so make our own.

            floatieNonVR = g.GetComponentInChildren<Image>(true)?.gameObject;
            if (floatieNonVR == null)
                return false;
            Main.mod.Logger.Log("Found the non VR float.");

            floatieNonVRText = floatieNonVR.GetComponentInChildren<TextMeshProUGUI>(true);
            if (floatieNonVRText == null)
                return false;
            Main.mod.Logger.Log("Found the non VR text.");

            floatieNonVRLine = floatieNonVR.GetComponentInChildren<TutorialLineNonVR>(true);
            if (floatieNonVRLine == null)
                return false;
            Main.mod.Logger.Log("Found the non VR line.");
            return true;
        }

        // Non VR floating text.
        public static void ChangeFloatieTextNonVR(string text)
        {
            floatieNonVRText.text = text;
        }

        public static void HideFloatNonVR()
        {
            floatieNonVR.SetActive(false);
            floatieNonVRText.text = string.Empty;
            floatieNonVRLine.attentionTransform = null;
        }

        public static void ShowFloatNonVR(string text)
        {
            HideFloatNonVR();
            floatieNonVRText.text = text;
            floatieNonVR.SetActive(true);
        }

        public static void UpdateAttentionTransformNonVR(Transform attentionTransform)
        {
            // if (floatieNonVR.activeInHierarchy)
            floatieNonVRLine.attentionTransform = attentionTransform;
        }

        // VR floating text.
        public static void ChangeFloatieTextVR(string text)
        {
            if (floatieVR != null)
                floatieVR.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
        }

        public static void HideFloatVR()
        {
            if (floatieVR != null)
                Object.Destroy(floatieVR);
        }

        public static void ShowFloatVR(string text)
        {
            HideFloatVR();
            if (!string.IsNullOrEmpty(text))
            {
                Transform eyesTransform = PlayerManager.PlayerCamera?.transform;
                if (eyesTransform == null)
                    return;
                Vector3 position = eyesTransform.position + eyesTransform.forward * 1.5f;
                Transform parent;
                if (VRManager.IsVREnabled())
                    parent = VRTK_DeviceFinder.PlayAreaTransform();
                else // Shouldn't be case for this mod.
                    parent = (PlayerManager.Car ? PlayerManager.Car.interior : (SingletonBehaviour<WorldMover>.Exists ? SingletonBehaviour<WorldMover>.Instance.originShiftParent : null));
                floatieVR = (Object.Instantiate(Resources.Load("tutorial_floatie"), position, Quaternion.identity, parent) as GameObject);
                floatieVR.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
            }
        }

        public static void UpdateAttentionTransformVR(Transform attentionTransform)
        {
            if (floatieVR != null)
                floatieVR.GetComponent<Floatie>().attentionPoint = attentionTransform;
        }
    }

    [HarmonyPatch(typeof(FloatieWithAnimation), "Start")]
    class FloatieWithAnimation_Start_Patch
    {
        static void Postfix(LineRenderer ___line)
        {
            ___line.startWidth = Floaties.START_WIDTH;
            ___line.endWidth = Floaties.END_WIDTH;
            ___line.material.mainTexture = Floaties.pointerTexture;
        }
    }

    [HarmonyPatch(typeof(TutorialLineNonVR), "Start")]
    class TutorialLineNonVR_Start_Patch
    {
        static void Postfix(LineRenderer ___line)
        {
            ___line.startWidth = Floaties.START_WIDTH;
            ___line.endWidth = Floaties.END_WIDTH;
            ___line.material.mainTexture = Floaties.pointerTexture;
        }
    }
}
