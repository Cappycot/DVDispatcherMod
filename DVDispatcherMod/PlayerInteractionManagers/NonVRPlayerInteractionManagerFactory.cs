using DV.Interaction;

namespace DVDispatcherMod.PlayerInteractionManagers {
    public static class NonVRPlayerInteractionManagerFactory {
        public static IPlayerInteractionManager TryCreate() {
            var grabber = PlayerManager.PlayerTransform?.GetComponentInChildren<Grabber>();
            if (grabber == null) {
                return null;
            }
            return new NonVRPlayerInteractionManager(grabber);
        }
    }
}