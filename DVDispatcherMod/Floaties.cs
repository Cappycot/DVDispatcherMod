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
        private static GameObject floatieNonVR;
        private static TutorialLineNonVR floatieNonVRLine;
        private static TextMeshProUGUI floatieNonVRText;

        // VR Floating Text
        private static GameObject floatieVR;

        // Floating Pointer
        private const float START_WIDTH = 0.0025f; // Original = 0.005f;
        private const float END_WIDTH = 0.01f; // Original = 0.02f;
        private static Texture2D pointerTexture;

        // Hide and show floating text.
        public delegate void ChangeFloatieTextDelegate(string text);
        public static ChangeFloatieTextDelegate ChangeFloatieText;
        public delegate void HideFloatieDelegate();
        public static HideFloatieDelegate HideFloatie;
        public delegate void ShowFloatieDelegate(string text);
        public static ShowFloatieDelegate ShowFloatie;
        public delegate void UpdateAttentionTransformDelegate(Transform attentionTransform);
        public static UpdateAttentionTransformDelegate UpdateAttentionTransform;

        public static bool Initialize()
        {
            if (pointerTexture == null)
            {
                pointerTexture = new Texture2D(256, 1);
                // Note: ImageConversion.LoadImage automatically invokes Apply.
                ImageConversion.LoadImage(pointerTexture, File.ReadAllBytes(Main.mod.Path + "tutorial_UI_gradient_opaque.png"));
            }

            if (VRManager.IsVREnabled())
            {
                ChangeFloatieText = ChangeFloatieTextVR;
                HideFloatie = HideFloatVR;
                ShowFloatie = ShowFloatVR;
                UpdateAttentionTransform = UpdateAttentionTransformVR;
            }
            else if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !InventoryStartingItems.itemsLoaded)
                return false;
            else
            {
                GameObject g = GameObject.Find("[NonVRFloatie]");
                if (g == null)
                {
                    SceneManager.LoadScene("non_vr_ui_floatie", LoadSceneMode.Additive);
                    g = GameObject.Find("[NonVRFloatie]");
                }
                
                if (g == null)
                    return false;
                g = GameObject.Instantiate(g); // The tutorial sequence destroys non VR floaties, so make our own.

                Image i = g.GetComponentInChildren<Image>(true);
                if (i == null)
                    return false;
                floatieNonVR = i.gameObject;
                Main.mod.Logger.Log("Found the non VR float.");

                floatieNonVRText = floatieNonVR.GetComponentInChildren<TextMeshProUGUI>(true);
                if (floatieNonVRText == null)
                    return false;
                Main.mod.Logger.Log("Found the non VR text.");

                floatieNonVRLine = floatieNonVR.GetComponentInChildren<TutorialLineNonVR>(true);
                if (floatieNonVRLine == null)
                    return false;
                Main.mod.Logger.Log("Found the non VR line.");

                ChangeFloatieText = ChangeFloatieTextNonVR;
                HideFloatie = HideFloatNonVR;
                ShowFloatie = ShowFloatNonVR;
                UpdateAttentionTransform = UpdateAttentionTransformNonVR;
            }

            Main.mod.Logger.Log("Floaties have been set up.");
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
            // TODO: Fiddle with LineRenderer make sure it's working then delete this section.
            LineRenderer lr = floatieNonVRLine.GetComponent<LineRenderer>();
            if (lr == null)
            {
                Main.mod.Logger.Log("The non VR LineRenderer is null for some reason.");
            }
            else
            {
                lr.startWidth = START_WIDTH;
                lr.endWidth = END_WIDTH;
                lr.material.mainTexture = pointerTexture;
            }
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
                UnityEngine.Object.Destroy(floatieVR);
        }

        public static void ShowFloatVR(string text)
        {
            HideFloatVR();
            if (!string.IsNullOrEmpty(text))
            {
                // TODO: Check if PlayerCamera is nullable.
                Transform eyesTransform = PlayerManager.PlayerCamera?.transform;
                if (eyesTransform == null)
                    return;
                Vector3 position = eyesTransform.position + eyesTransform.forward * 1.5f;
                Transform parent;
                if (VRManager.IsVREnabled())
                {
                    parent = VRTK_DeviceFinder.PlayAreaTransform();
                }
                else
                {
                    parent = (PlayerManager.Car ? PlayerManager.Car.interior : (SingletonBehaviour<WorldMover>.Exists ? SingletonBehaviour<WorldMover>.Instance.originShiftParent : null));
                }
                floatieVR = (Object.Instantiate(Resources.Load("tutorial_floatie"), position, Quaternion.identity, parent) as GameObject);
                floatieVR.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
            }
        }

        public static void UpdateAttentionTransformVR(Transform attentionTransform)
        {
            if (floatieVR != null)
            {
                floatieVR.GetComponent<Floatie>().attentionPoint = attentionTransform;
                // TODO: Fiddle with LineRenderer make sure it's working then delete this section.
                LineRenderer lr = floatieVR.GetComponent<LineRenderer>();
                // LineRenderer doesn't exist until VR floatie is fully expanded for some reason.
                /*if (lr == null)
                {
                    Main.mod.Logger.Log("The VR LineRenderer is null for some reason.");
                }
                else*/
                if (lr != null)
                {
                    lr.startWidth = START_WIDTH;
                    lr.endWidth = END_WIDTH;
                    lr.material.mainTexture = pointerTexture;
                }
            }
        }
    }
}
