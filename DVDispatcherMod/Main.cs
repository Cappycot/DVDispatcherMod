using Harmony12;
using System.Reflection;
using DVDispatcherMod.DispatcherHintManagers;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.HarmonyPatches;
using DVDispatcherMod.PlayerInteractionManagers;
using UnityModManagerNet;

/*
 * Visual Studio puts braces on a different line? How do y'all live like this?
 * 
 * Notes:
 * - Lists of cars fully vs partially on a logic track are mutually exclusive.
 */
namespace DVDispatcherMod {
    static class Main {
        // Time between forced dispatcher updates.
        private const float POINTER_INTERVAL = 1;

        private static float _timer;
        private static int _counter;

        private static DispatcherHintManager _dispatcherHintManager;

        public static UnityModManager.ModEntry ModEntry { get; private set; }

        static bool Load(UnityModManager.ModEntry modEntry) {
            ModEntry = modEntry;
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;

            PointerTexture.Initialize();

            return true;
        }

        // TODO: Make sure OnToggle works.
        static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled) {
            if (_dispatcherHintManager != null) {
                _dispatcherHintManager.SetIsEnabled(isEnabled);
            }

            ModEntry.Logger.Log(string.Format("isEnabled toggled to {0}.", isEnabled));

            return true;
        }

        static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            _timer += delta;

            Main.ModEntry.Logger.Log("OnUpdate.");

            if (_dispatcherHintManager == null) {
                _dispatcherHintManager = TryCreateDispatcherHintManager();
                if (_dispatcherHintManager == null) {
                    return;
                }

                mod.Logger.Log(string.Format("Floaties have been set up, total time elapsed: {0:0.00} seconds.", _timer));
            }

            if (_timer > POINTER_INTERVAL) {
                _counter++;
                _timer %= POINTER_INTERVAL;

                _dispatcherHintManager.SetCounter(_counter);
            }
        }

        private static DispatcherHintManager TryCreateDispatcherHintManager() {
            if (VRManager.IsVREnabled()) {
                var playerInteractionManager = VRPlayerInteractionManagerFactory.TryCreate();
                if (playerInteractionManager == null) {
                    return null;
                }

                var dispatchHintShower = new VRDispatchHintShower();
                return new DispatcherHintManager(playerInteractionManager, dispatchHintShower);
            } else {
                return NonVRDispatchHintManagerFactory.TryCreate();
            }
        }
    }
}
