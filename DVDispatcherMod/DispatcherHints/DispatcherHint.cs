using JetBrains.Annotations;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHints {
    public class DispatcherHint {
        public DispatcherHint(string text, [CanBeNull] Vector3? attentionPoint = null) {
            Text = text;
            AttentionPoint = attentionPoint;
        }

        public string Text { get; }
        [CanBeNull] public Vector3? AttentionPoint { get; }
    }
}