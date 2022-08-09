namespace DVDispatcherMod.PlayerInteractionManagers {
    public static class NonVRPlayerInteractionManagerFactory {
        public static IPlayerInteractionManager TryCreate() {
            var grabber = PlayerManager.PlayerTransform?.GetComponentInChildren<Grabber>();
            var inventory = SingletonBehaviour<Inventory>.Instance;
            if (grabber == null || inventory == null) {
                return null;
            }
            return new NonVRPlayerInteractionManager(grabber, inventory);
        }
    }
}