using Harmony12;
using System.Reflection;
using DVDispatcherMod.DispatcherHintManagers;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using UnityModManagerNet;

namespace DVDispatcherMod {
    static class Main {
        private const float SETUP_INTERVAL = 1;
        private const float POINTER_INTERVAL = 1; // Time between forced dispatcher updates.

        private static float _timer;
        private static int _counter;
        private static bool _isEnabled;

        private static DispatcherHintManager _dispatcherHintManager;

        public static UnityModManager.ModEntry ModEntry { get; private set; }

#pragma warning disable IDE0051 // Remove unused private members
        static bool Load(UnityModManager.ModEntry modEntry) {
#pragma warning restore IDE0051 // Remove unused private members
            ModEntry = modEntry;
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled) {
            _isEnabled = isEnabled;

            ModEntry.Logger.Log(string.Format("isEnabled toggled to {0}.", isEnabled));

            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            if (IsModEnabledAndWorldReadyForInteraction()) {
                _timer += delta;

                if (_dispatcherHintManager == null) {
                    if (_timer > SETUP_INTERVAL) {
                        _timer %= SETUP_INTERVAL;

                        _dispatcherHintManager = TryCreateDispatcherHintManager();
                        if (_dispatcherHintManager != null) {
                            mod.Logger.Log("Dispatcher hint manager created.");
                        }
                    }
                }

                if (_dispatcherHintManager != null) {
                    if (_timer > POINTER_INTERVAL) {
                        _counter++;
                        _timer %= POINTER_INTERVAL;

                        _dispatcherHintManager.SetCounter(_counter);
                    }
                }
            } else {
                if (_dispatcherHintManager != null) {
                    _dispatcherHintManager.Dispose();
                    _dispatcherHintManager = null;
                    ModEntry.Logger.Log("Disposed dispatcher hint manager.");
                }
            }
        }

        private static bool IsModEnabledAndWorldReadyForInteraction() {
            if (!_isEnabled) {
                return false;
            }
            if (LoadingScreenManager.IsLoading) {
                return false;
            }
            if (!WorldStreamingInit.IsLoaded) {
                return false;
            }
            return true;
        }

        private static DispatcherHintManager TryCreateDispatcherHintManager() {
            if (VRManager.IsVREnabled()) {
                var playerInteractionManager = VRPlayerInteractionManagerFactory.TryCreate();
                if (playerInteractionManager == null) {
                    return null;
                }

                var dispatcherHintShower = new DispatcherHintShower();
                return new DispatcherHintManager(playerInteractionManager, dispatcherHintShower, new TaskOverviewGenerator());
            } else {
                var playerInteractionManager = NonVRPlayerInteractionManagerFactory.TryCreate();
                if (playerInteractionManager == null) {
                    return null;
                }

                var dispatcherHintShower = new DispatcherHintShower();
                return new DispatcherHintManager(playerInteractionManager, dispatcherHintShower, new TaskOverviewGenerator());
            }
        }
    }
}
