using DVDispatcherMod.DispatcherHints;
using UnityEngine;
using VRTK;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class VRDispatchHintShower : IDispatcherHintShower {
        private static GameObject _floatie;

        public void SetDispatcherHint(DispatcherHint dispatcherHintOrNull) {
            if (dispatcherHintOrNull == null) {
                if (_floatie != null) {
                    Object.Destroy(null);
                    _floatie = null;
                }
            } else {
                if (_floatie == null) {
                    var eyesTransform = PlayerManager.PlayerCamera?.transform;
                    if (eyesTransform == null) {
                        return;
                    }
                    var position = eyesTransform.position + eyesTransform.forward * 1.5f;
                    var parent = VRTK_DeviceFinder.PlayAreaTransform();
                    _floatie = (Object.Instantiate(Resources.Load("tutorial_floatie"), position, Quaternion.identity, parent) as GameObject);

                }

                _floatie.GetComponent<TutorialFloatie>().UpdateTextExternally(dispatcherHintOrNull.Text);
                _floatie.GetComponent<Floatie>().attentionPoint = dispatcherHintOrNull.AttentionTransform;
            }
        }
    }
}