using UnityEngine;

namespace DVDispatcherMod {
    public class DispatcherHint {
        public DispatcherHint(string text, Transform attentionTransform) {
            Text = text;
            AttentionTransform = attentionTransform;
        }

        public string Text { get; }
        public Transform AttentionTransform { get; }
    }

    public interface IDispatcherHintShower {
        void SetDispatcherHint(DispatcherHint dispatcherHintOrNull);
    }
}