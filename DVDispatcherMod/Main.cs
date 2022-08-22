using Harmony12;
using System.Collections.Generic;
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
namespace DVDispatcherMod
{
    static class Main
    {
        // Time between forced dispatcher updates.
        public const float POINTER_INTERVAL = 1;

        // Store job booklet page-specific helpful information.
        public static Dictionary<string, IDispatch[]> jobDispatches = new Dictionary<string, IDispatch[]>();

        public static UnityModManager.ModEntry mod;
        private static bool floatLoaded;
        private static bool showFloat;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            mod.OnToggle = OnToggle;
            mod.OnUpdate = OnUpdate;
            floatLoaded = false;
            showFloat = true;
            Floaties.Initialize();
            return true;
        }

        // TODO: Make sure OnToggle works.
        static bool OnToggle(UnityModManager.ModEntry _, bool active)
        {
            showFloat = active;
            mod.Logger.Log(string.Format("showFloats toggled to {0}.", showFloat));
            if (!showFloat)
            {
                Floaties.HideFloatie();
                holdingRight = null;
                // TODO: Check which job data needs to be reset.
                showing = false;
            }
            return true;
        }

        private static bool listenersSetup = false;

        // Job Holdings
        private static bool showing = false;
        private static int counter = 0;
        private static float timer = 0;
        // private static IDispatch holdingLeft;
        private static JobDispatch holdingRight;

        static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            timer += delta;
            if (!listenersSetup)
            {
                // eyesTransform = PlayerManager.PlayerCamera.transform;
                if (VRManager.IsVREnabled())
                {
                    VRTK_InteractGrab lGrab = VRTK_DeviceFinder.GetControllerLeftHand(true)?.transform.GetComponentInChildren<VRTK_InteractGrab>();
                    VRTK_InteractGrab rGrab = VRTK_DeviceFinder.GetControllerRightHand(true)?.transform.GetComponentInChildren<VRTK_InteractGrab>();
                    if (lGrab == null || rGrab == null)
                        return;
                    lGrab.ControllerGrabInteractableObject += OnItemGrabbedLeftVR;
                    lGrab.ControllerStartUngrabInteractableObject += OnItemUngrabbedLeftVR;
                    rGrab.ControllerGrabInteractableObject += OnItemGrabbedRightVR;
                    rGrab.ControllerStartUngrabInteractableObject += OnItemUngrabbedRightVR;
                }
                else
                {
                    if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !SingletonBehaviour<Inventory>.Instance)
                        return;
                    else if (!floatLoaded)
                    {
                        floatLoaded = Floaties.InitFloatieNonVR();
                        return;
                    }
                    Grabber grab = PlayerManager.PlayerTransform?.GetComponentInChildren<Grabber>();
                    if (grab == null || SingletonBehaviour<Inventory>.Instance == null)
                        return;
                    grab.Grabbed += OnItemGrabbedRightNonVR;
                    grab.Released += OnItemUngrabbedRightNonVR;
                    SingletonBehaviour<Inventory>.Instance.ItemAddedToInventory += OnItemAddedToInventory;
                }

                mod.Logger.Log(string.Format("Floaties have been set up, total time elapsed: {0:0.00} seconds.", timer));
                listenersSetup = true;
            }
            else
            {
                if (showing)
                {
                    if (holdingRight == null || !showFloat)
                    {
                        Floaties.HideFloatie();
                        showing = false;
                    }
                    else
                    {
                        if (timer > POINTER_INTERVAL)
                        {
                            counter++;
                            timer %= POINTER_INTERVAL;
                            // Calculate tracks that cars are on.
                            holdingRight.UpdateJobCars();
                            holdingRight.UpdateJobPrivilege();
                            Floaties.ChangeFloatieText(holdingRight.GetFloatieText(counter));
                            Floaties.UpdateAttentionTransform(holdingRight.GetPointerAt(counter));
                        }
                    }
                }
                else if (!showing && holdingRight != null && showFloat)
                {
                    // TODO: Read job information and process text accordingly.
                    Floaties.ShowFloatie(holdingRight.GetFloatieText(counter));
                    Floaties.UpdateAttentionTransform(holdingRight.GetPointerAt(counter));
                    showing = true;
                    timer = 0;
                }
            }
        }

        // Actual Grab Handlers
        static void OnItemGrabbedLeft(InventoryItemSpec iis)
        {
            if (iis == null)
                return;
            // mod.Logger.Log(string.Format("Picked up a(n) {0} in the left hand.", iis.itemName));
            // JobOverview jo = iis.GetComponent<JobOverview>();
            // if (jo != null)
            // holdingLeft = jo.job;
        }

        static void OnItemGrabbedRight(InventoryItemSpec iis)
        {
            if (iis == null)
                return;
            // mod.Logger.Log(string.Format("Picked up a(n) {0} in the right hand.", iis.itemName));
            JobOverview jo = iis.GetComponent<JobOverview>();
            if (jo != null)
            {
                holdingRight = new JobDispatch(jo.job);
                showing = false;
            }
            else
            {
                JobBooklet jb = iis.GetComponent<JobBooklet>();
                if (jb != null)
                {
                    holdingRight = new JobDispatch(jb.job);
                    showing = false;
                }
            }
        }

        static void OnItemUngrabbedLeft(InventoryItemSpec iis)
        {
            // holdingLeft = null;
        }

        static void OnItemUngrabbedRight(InventoryItemSpec iis)
        {
            holdingRight = null;
        }

        // Grab Listeners
        static void OnItemAddedToInventory(GameObject o, int _)
        {
            OnItemUngrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightNonVR(GameObject o)
        {
            OnItemGrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightNonVR(GameObject o)
        {
            OnItemUngrabbedRight(o?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedLeftVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemGrabbedLeft(e.target?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemGrabbedRight(e.target?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedLeftVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemUngrabbedLeft(e.target?.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemUngrabbedRight(e.target?.GetComponent<InventoryItemSpec>());
        }
    }
}