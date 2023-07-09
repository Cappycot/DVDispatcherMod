using DVDispatcherMod.DispatcherHints;
using TMPro;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class NonVRDispatcherHintShower : IDispatcherHintShower {
        private readonly GameObject _floatie;
        private readonly TextMeshProUGUI _floatieText;
        private readonly NonVRLineRendererController _floatieLine;
        
        private readonly Transform _attentionLineTransform;

        private bool _currentlyShowing;

        public NonVRDispatcherHintShower(GameObject floatie, TextMeshProUGUI floatieText, NonVRLineRendererController floatieLine) {
            _floatie = floatie;
            _floatieText = floatieText;
            _floatieLine = floatieLine;

            // transforms cannot be instantiated directly, they always live within a game object. thus we create a single (unnecessary) game object and keep it's transform
            var transformGivingGameObject = new GameObject();
            _attentionLineTransform = transformGivingGameObject.transform;
        }

        public void SetDispatcherHint(DispatcherHint dispatcherHintOrNull) {
            if (dispatcherHintOrNull == null) {
                if (_currentlyShowing) {
                    _floatie.SetActive(false);
                    _floatieText.text = string.Empty;
                    _floatieLine.attentionTransform = null;

                    _currentlyShowing = false;
                }
            } else {
                if (_currentlyShowing) {
                    _floatieText.text = dispatcherHintOrNull.Text;
                    SetFloatieLineAttentionTransform(dispatcherHintOrNull.AttentionPoint);
                } else {
                    _floatie.SetActive(false); // dunno, was in original code like that.

                    _floatieText.text = dispatcherHintOrNull.Text;
                    SetFloatieLineAttentionTransform(dispatcherHintOrNull.AttentionPoint);

                    _floatie.SetActive(true);

                    _currentlyShowing = true;
                }
            }
        }

        private void SetFloatieLineAttentionTransform(Vector3? attentionPoint) {
            if (attentionPoint == null) {
                _floatieLine.attentionTransform = null;
            } else {
                _attentionLineTransform.position = attentionPoint.Value;
                _floatieLine.attentionTransform = _attentionLineTransform;
            }
        }
    }
}