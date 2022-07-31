using Harmony12;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DVDispatcherMod {
    ////public static class Floaties {

    ////    // VR Floating Text
    ////    private static GameObject _floatieVr;

    ////    // Floating Pointer
    ////    public const float START_WIDTH = 0.0025f; // Original = 0.005f;
    ////    public const float END_WIDTH = 0.01f; // Original = 0.02f;
    ////    public static Texture2D pointerTexture;

    ////    // Hide and show floating text.
    ////    public delegate void ChangeFloatieTextDelegate(string text);
    ////    public static ChangeFloatieTextDelegate ChangeFloatieText;
    ////    public delegate void HideFloatieDelegate();
    ////    public static HideFloatieDelegate HideFloatie;
    ////    public delegate void ShowFloatieDelegate(string text);
    ////    public static ShowFloatieDelegate ShowFloatie;
    ////    public delegate void UpdateAttentionTransformDelegate(Transform attentionTransform);
    ////    public static UpdateAttentionTransformDelegate UpdateAttentionTransform;

    ////    public static void Initialize() {
    ////        pointerTexture = new Texture2D(256, 1);
    ////        // Note: ImageConversion.LoadImage automatically invokes Apply.
    ////        ImageConversion.LoadImage(pointerTexture, File.ReadAllBytes(Main.ModEntry.Path + "tutorial_UI_gradient_opaque.png"));
    ////        // Solely based on command line args, so fine to init ASAP.
    ////        if (VRManager.IsVREnabled()) {
    ////            ChangeFloatieText = ChangeFloatieTextVR;
    ////            HideFloatie = HideFloatVR;
    ////            ShowFloatie = ShowFloatVR;
    ////            UpdateAttentionTransform = UpdateAttentionTransformVR;
    ////        } else {
    ////            ChangeFloatieText = ChangeFloatieTextNonVR;
    ////            HideFloatie = HideFloatNonVR;
    ////            ShowFloatie = ShowFloatNonVR;
    ////            UpdateAttentionTransform = UpdateAttentionTransformNonVR;
    ////        }
    ////    }

    ////    // VR floating text.
    ////    public static void ChangeFloatieTextVR(string text) {
    ////        if (_floatieVr != null) {
    ////            _floatieVr.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
    ////        }
    ////    }

    ////    public static void HideFloatVR() {
    ////        if (_floatieVr != null) {
    ////            Object.Destroy(_floatieVr);
    ////        }
    ////    }

    ////    public static void ShowFloatVR(string text) {
    ////        HideFloatVR();
    ////        if (!string.IsNullOrEmpty(text)) {
    ////            var eyesTransform = PlayerManager.PlayerCamera?.transform;
    ////            if (eyesTransform == null) {
    ////                return;
    ////            }
    ////            var position = eyesTransform.position + eyesTransform.forward * 1.5f;
    ////            Transform parent;
    ////            if (VRManager.IsVREnabled()) {
    ////                parent = VRTK_DeviceFinder.PlayAreaTransform();
    ////            } else // Shouldn't be case for this mod.
    ////            {
    ////                parent = (PlayerManager.Car ? PlayerManager.Car.interior : (SingletonBehaviour<WorldMover>.Exists ? SingletonBehaviour<WorldMover>.Instance.originShiftParent : null));
    ////            }
    ////            _floatieVr = (Object.Instantiate(Resources.Load("tutorial_floatie"), position, Quaternion.identity, parent) as GameObject);
    ////            _floatieVr.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
    ////        }
    ////    }

    ////    public static void UpdateAttentionTransformVR(Transform attentionTransform) {
    ////        if (_floatieVr != null) {
    ////            _floatieVr.GetComponent<Floatie>().attentionPoint = attentionTransform;
    ////        }
    ////    }
    ////}
}
