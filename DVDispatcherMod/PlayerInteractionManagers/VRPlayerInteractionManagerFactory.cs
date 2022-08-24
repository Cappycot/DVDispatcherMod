using VRTK;

namespace DVDispatcherMod.PlayerInteractionManagers {
    internal static class VRPlayerInteractionManagerFactory {
        public static IPlayerInteractionManager TryCreate() {
            // this could probably be extended to support both hands easily by handling both grabbers and defining a priority hand.
            var rGrab = VRTK_DeviceFinder.GetControllerRightHand(true)?.transform.GetComponentInChildren<VRTK_InteractGrab>();
            if (rGrab == null) {
                return null;
            }

            return new VRPlayerInteractionManager(rGrab);
        }

    }
}