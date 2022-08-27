using DVDispatcherMod.DispatcherHints;
using TMPro;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class NonVRDispatcherHintShower : IDispatcherHintShower {
        private readonly GameObject _floatie;
        private readonly TextMeshProUGUI _floatieText;
        private readonly TutorialLineNonVR _floatieLine;

        private bool _currentlyShowing;

        public NonVRDispatcherHintShower(GameObject floatie, TextMeshProUGUI floatieText, TutorialLineNonVR floatieLine) {
            _floatie = floatie;
            _floatieText = floatieText;
            _floatieLine = floatieLine;
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
                    _floatieLine.attentionTransform = dispatcherHintOrNull.AttentionTransformOrNull;
                } else {
                    _floatie.SetActive(false); // dunno, was in original code like that.

                    _floatieText.text = dispatcherHintOrNull.Text;
                    _floatieLine.attentionTransform = dispatcherHintOrNull.AttentionTransformOrNull;

                    _floatie.SetActive(true);

                    _currentlyShowing = true;
                }
            }
        }
    }
}