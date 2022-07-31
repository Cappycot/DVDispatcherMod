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

        private static bool _floatLoaded;
        private static bool _isEnabled;

        static bool Load(UnityModManager.ModEntry modEntry) {
            ModEntry = modEntry;
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            _floatLoaded = false;
            _isEnabled = true;
            Floaties.Initialize();
            return true;
        }

        // TODO: Make sure OnToggle works.
        static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled) {
            _isEnabled = isEnabled;
            ModEntry.Logger.Log(string.Format("isEnabled toggled to {0}.", _isEnabled));
            if (!_isEnabled) {
                Floaties.HideFloatie();
                _holdingRight = null;
                // TODO: Check which job data needs to be reset.
                _showing = false;
            }
            return true;
        }

        private static bool _listenersSetup;

        // Job Holdings
        private static bool _showing;
        private static int _counter;
        private static float _timer;

        private static JobDispatch _holdingRight;

        static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            _timer += delta;

            if (!_listenersSetup) {
                // eyesTransform = PlayerManager.PlayerCamera.transform;
                if (VRManager.IsVREnabled()) {
                    var rGrab = VRTK_DeviceFinder.GetControllerRightHand(true)?.transform.GetComponentInChildren<VRTK_InteractGrab>();
                    if (rGrab == null)
                        return;
                    rGrab.ControllerGrabInteractableObject += OnItemGrabbedRightVR;
                    rGrab.ControllerStartUngrabInteractableObject += OnItemUngrabbedRightVR;
                } else {
                    if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !SingletonBehaviour<Inventory>.Exists)
                        return;
                    else if (!_floatLoaded) {
                        _floatLoaded = Floaties.InitFloatieNonVR();
                        return;
                    }
                    var grab = PlayerManager.PlayerTransform?.GetComponentInChildren<Grabber>();
                    if (grab == null || SingletonBehaviour<Inventory>.Instance == null)
                        return;
                    grab.Grabbed += OnItemGrabbedRightNonVR;
                    grab.Released += OnItemUngrabbedRightNonVR;
                    SingletonBehaviour<Inventory>.Instance.ItemAddedToInventory += OnItemAddedToInventory;
                }

                mod.Logger.Log(string.Format("Floaties have been set up, total time elapsed: {0:0.00} seconds.", _timer));
                _listenersSetup = true;
            } else {
                if (_showing) {
                    if (_holdingRight == null || !_isEnabled) {
                        Floaties.HideFloatie();
                        _showing = false;
                    } else {
                        if (_timer > POINTER_INTERVAL) {
                            _counter++;
                            _timer %= POINTER_INTERVAL;
                            // Calculate tracks that cars are on.
                            _holdingRight.UpdateJobCars();
                            _holdingRight.UpdateJobPrivilege();
                            Floaties.ChangeFloatieText(_holdingRight.GetFloatieText(_counter));
                            Floaties.UpdateAttentionTransform(_holdingRight.GetPointerAt(_counter));
                        }
                    }
                } else if (!_showing && _holdingRight != null && _isEnabled) {
                    // TODO: Read job information and process text accordingly.
                    Floaties.ShowFloatie(_holdingRight.GetFloatieText(_counter));
                    Floaties.UpdateAttentionTransform(_holdingRight.GetPointerAt(_counter));
                    _showing = true;
                    _timer = 0;
                }
            }
        }

        static void OnItemGrabbedRight(InventoryItemSpec iis) {
            if (iis == null)
                return;
            // mod.Logger.Log(string.Format("Picked up a(n) {0} in the right hand.", iis.itemName));
            var jo = iis.GetComponent<JobOverview>();
            if (jo != null) {
                _holdingRight = new JobDispatch(jo.job);
                _showing = false;
            } else {
                var jb = iis.GetComponent<JobBooklet>();
                if (jb != null) {
                    _holdingRight = new JobDispatch(jb.job);
                    _showing = false;
                }
            }
        }

        static void OnItemUngrabbedRight(InventoryItemSpec iis) {
            _holdingRight = null;
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
