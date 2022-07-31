using System.IO;
using UnityEngine;

namespace DVDispatcherMod {
    public static class PointerTexture {
        public static Texture2D Texture { get; private set; }

        public static void Initialize() {
            var pointerTexture = new Texture2D(256, 1);
            // Note: ImageConversion.LoadImage automatically invokes Apply.
            ImageConversion.LoadImage(pointerTexture, File.ReadAllBytes(Main.ModEntry.Path + "tutorial_UI_gradient_opaque.png"));

            Texture = pointerTexture;
        }
    }
}