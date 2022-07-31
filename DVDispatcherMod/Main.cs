using Harmony12;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using VRTK;

/*
 * Visual Studio puts braces on a different line? How do y'all live like this?
 * 
 * Notes:
 * - Lists of cars fully vs partially on a logic track are mutually exclusive.
 */
namespace DVDispatcherMod {
    static class Main {
        // Time between forced dispatcher updates.
        public const float POINTER_INTERVAL = 1;

        public static UnityModManager.ModEntry ModEntry { get; private set; }

        private static bool _isEnabled;

        static bool Load(UnityModManager.ModEntry modEntry) {
            ModEntry = modEntry;
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            _isEnabled = true;
            ////Floaties.Initialize();

            PointerTexture.Initialize();

            return true;
        }

        // TODO: Make sure OnToggle works.
        static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled) {
            _isEnabled = isEnabled;

            if (_dispatchHintShower != null) {
                UpdateDispatcherHint();
            }

            ModEntry.Logger.Log(string.Format("isEnabled toggled to {0}.", _isEnabled));

            return true;
        }

        private static bool _listenersSetup;

        private static IDispatcherHintShower _dispatchHintShower;

        // Job Holdings
        private static int _counter;
        private static float _timer;

        private static JobDispatch _holdingRight;

        static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            _timer += delta;

            if (!_listenersSetup) {
                // eyesTransform = PlayerManager.PlayerCamera.transform;
                if (VRManager.IsVREnabled()) {
                    var rGrab = VRTK_DeviceFinder.GetControllerRightHand(true)?.transform.GetComponentInChildren<VRTK_InteractGrab>();
                    if (rGrab == null) {
                        return;
                    }
                    rGrab.ControllerGrabInteractableObject += OnItemGrabbedRightVR;
                    rGrab.ControllerStartUngrabInteractableObject += OnItemUngrabbedRightVR;
                } else {
                    if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !SingletonBehaviour<Inventory>.Exists) {
                        return;
                    } else if (_dispatchHintShower == null) {
                        _dispatchHintShower = NonVRDispatcherHintShowerFactory.TryCreate();
                        return;
                    }
                    var grab = PlayerManager.PlayerTransform?.GetComponentInChildren<Grabber>();
                    if (grab == null || SingletonBehaviour<Inventory>.Instance == null) {
                        return;
                    }
                    grab.Grabbed += OnItemGrabbedRightNonVR;
                    grab.Released += OnItemUngrabbedRightNonVR;
                    SingletonBehaviour<Inventory>.Instance.ItemAddedToInventory += OnItemAddedToInventory;
                }

                mod.Logger.Log(string.Format("Floaties have been set up, total time elapsed: {0:0.00} seconds.", _timer));
                _listenersSetup = true;
            } else {
                if (_timer > POINTER_INTERVAL) {
                    _counter++;
                    _timer %= POINTER_INTERVAL;

                    UpdateDispatcherHint();

                    // TODO set timer to zero in case of newly showing floatie, so a full second can pass until index switches
                }
            }
        }

        private static void UpdateDispatcherHint() {
            var currentHint = GetCurrentDispatcherHint();
            _dispatchHintShower.SetDispatcherHint(currentHint);
        }

        private static DispatcherHint GetCurrentDispatcherHint() {
            if (!_isEnabled) {
                return null;
            }

            DispatcherHint currentHint;
            if (_holdingRight == null) {
                currentHint = null;
            } else {
                // Calculate tracks that cars are on.
                _holdingRight.UpdateJobCars();
                _holdingRight.UpdateJobPrivilege();

                currentHint = new DispatcherHint(_holdingRight.GetFloatieText(_counter), _holdingRight.GetPointerAt(_counter));
            }
            return currentHint;
        }

        static void OnItemGrabbedRight(InventoryItemSpec iis) {
            if (iis == null) {
                return;
            }

            // mod.Logger.Log(string.Format("Picked up a(n) {0} in the right hand.", iis.itemName));
            var jo = iis.GetComponent<JobOverview>();
            if (jo != null) {
                _holdingRight = new JobDispatch(jo.job);
            } else {
                var jb = iis.GetComponent<JobBooklet>();
                if (jb != null) {
                    _holdingRight = new JobDispatch(jb.job);
                }
            }

            UpdateDispatcherHint();

            _timer = 0;
        }

        static void OnItemUngrabbedRight(InventoryItemSpec iis) {
            _holdingRight = null;
            UpdateDispatcherHint();
        }

        // Grab Listeners
        static void OnItemAddedToInventory(GameObject o, int _) {
            OnItemUngrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightNonVR(GameObject o) {
            OnItemGrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightNonVR(GameObject o) {
            OnItemUngrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightVR(object sender, ObjectInteractEventArgs e) {
            OnItemGrabbedRight(e.target?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightVR(object sender, ObjectInteractEventArgs e) {
            OnItemUngrabbedRight(e.target?.GetComponent<InventoryItemSpec>());
        }
    }
}
