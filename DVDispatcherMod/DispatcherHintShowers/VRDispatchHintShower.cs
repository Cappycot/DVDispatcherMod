using DVDispatcherMod.DispatcherHints;
using UnityEngine;
using VRTK;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class VRDispatchHintShower : IDispatcherHintShower {
        private readonly Transform _attentionLineTransform;

        private GameObject _floatie;

        public VRDispatchHintShower() {
            // transforms cannot be instantiated directly, they always live within a game object. thus we create a single (unnecessary) game object and keep it's transform
            var transformGivingGameObject = new GameObject();
            _attentionLineTransform = transformGivingGameObject.transform;
        }

        public void SetDispatcherHint(DispatcherHint dispatcherHintOrNull) {
            if (dispatcherHintOrNull == null) {
                if (_floatie != null) {
                    Object.Destroy(_floatie);
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

                if (dispatcherHintOrNull.AttentionPoint != null) {
                    _attentionLineTransform.position = dispatcherHintOrNull.AttentionPoint.Value;
                    _floatie.GetComponent<Floatie>().attentionPoint = _attentionLineTransform;
                }
            }
        }
    }
}