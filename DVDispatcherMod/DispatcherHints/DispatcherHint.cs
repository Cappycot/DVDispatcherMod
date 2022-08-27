using JetBrains.Annotations;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHints {
    public class DispatcherHint {
        public DispatcherHint(string text, [CanBeNull] Transform attentionTransformOrNull = null) {
            Text = text;
            AttentionTransformOrNull = attentionTransformOrNull;
        }

        public string Text { get; }
        [CanBeNull] public Transform AttentionTransformOrNull { get; }
    }
}