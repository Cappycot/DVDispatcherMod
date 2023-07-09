using DV.InventorySystem;
using DV.Utils;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;

namespace DVDispatcherMod.DispatcherHintManagers {
    public static class NonVRDispatchHintManagerFactory {
        private static NonVRDispatcherHintShower _dispatcherHintShower;
        private static IPlayerInteractionManager _playerInteractionManager;

        public static DispatcherHintManager TryCreate() {
            if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || SingletonBehaviour<Inventory>.Instance == null) {
                Main.ModEntry.Logger.Log("Out1");
                return null;
            }

            _dispatcherHintShower = _dispatcherHintShower ?? NonVRDispatcherHintShowerFactory.TryCreate();
            if (_dispatcherHintShower == null) {
                Main.ModEntry.Logger.Log("Out2");
                return null;
            }

            _playerInteractionManager = _playerInteractionManager ?? NonVRPlayerInteractionManagerFactory.TryCreate();
            if (_playerInteractionManager == null) {
                Main.ModEntry.Logger.Log("Out3");
                return null;
            }

            return new DispatcherHintManager(_playerInteractionManager, _dispatcherHintShower);
        }
    }
}