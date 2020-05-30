using DV.Logic.Job;
using DV.ServicePenalty;
using Harmony12;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityModManagerNet;
using VRTK;

/*
 * Visual Studio puts braces on a different line? How do y'all live like this?
 */
namespace DVDispatcherMod
{
    static class Main
    {
        public static UnityModManager.ModEntry mod;

        // NonVR Floating Text
        private static GameObject floatieNonVR;
        private static TextMeshProUGUI nonVRFloatieText;
        private static TutorialLineNonVR floatieLine;
        
        // VR Floating Text
        private static Transform eyesTransform;
        private static GameObject floatieVR;

        // Floating Pointer
        private static readonly float startWidth = 0.0025f; // Original = 0.005f;
        private static readonly float endWidth = 0.01f; // Original = 0.02f;
        private static readonly float pointerInterval = 1f;
        private static Texture2D pointerTexture;

        // Hide and show floating text.
        private delegate void ChangeFloatieTextDelegate(string text);
        private static ChangeFloatieTextDelegate ChangeFloatieText;
        private delegate void HideFloatieDelegate();
        private static HideFloatieDelegate HideFloatie;
        private delegate void ShowFloatieDelegate(string text);
        private static ShowFloatieDelegate ShowFloatie;
        private delegate void UpdateAttentionTransformDelegate(Transform attentionTransform);
        private static UpdateAttentionTransformDelegate UpdateAttentionTransform;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll();
            mod = modEntry;
            mod.OnToggle = OnToggle;
            mod.OnUpdate = OnUpdate;
            pointerTexture = new Texture2D(256, 1);
            ImageConversion.LoadImage(pointerTexture, File.ReadAllBytes(mod.Path + "tutorial_UI_gradient_opaque.png"));
            showFloats = true;
            return true;
        }

        // TODO: Make sure OnToggle works.
        private static bool showFloats;
        static bool OnToggle(UnityModManager.ModEntry _, bool active)
        {
            showFloats = active;
            mod.Logger.Log(string.Format("showFloats toggled to {0}.", showFloats));
            if (!showFloats)
            {
                HideFloatie();
                holdingRight = null;
                // TODO: Check which job data needs to be reset.
                jobCars = null;
                showing = false;
            }
            return true;
        }

        private static bool sceneLoaded = false; // Desktop floating text scene.
        private static bool floatLoaded = false;
        private static bool listenersSetup = false;

        // Job Holdings
        private static bool showing = false;
        private static int counter = 0;
        private static float timer = 0;
        // private static Job holdingLeft;
        private static Job holdingRight;

        private static List<Car> jobCars;
        private static Dictionary<Car, bool> jobCarsUsed;
        private static List<TrainCar> consistPointers;
        private static List<TrackID> trackNames;
        private static List<Trainset> jobConsists;
        private static bool locoHooked = false;

        // TODO: Move float loaders to separate func if too big.
        // I'll make sure this function gets as big as YandereDev's update functions. Don't worry.
        static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            // The tutorial sequence waits for these items to load in first.
            if (!floatLoaded)
            {
                if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !InventoryStartingItems.itemsLoaded)
                {
                    return;
                }
                // what = new TutorialFloatie().gameObject;
                if (VRManager.IsVREnabled())
                {
                    // Setup VR floating text.
                    if (!LocomotionSetup.Initialized)
                        return;

                    ChangeFloatieText = ChangeFloatieTextVR;
                    HideFloatie = HideFloatVR;
                    ShowFloatie = ShowFloatVR;
                    UpdateAttentionTransform = UpdateAttentionTransformVR;
                }
                else
                {
                    // Setup desktop floating text.
                    if (!sceneLoaded)
                    {
                        SceneManager.LoadScene("non_vr_ui_floatie", LoadSceneMode.Additive);
                        sceneLoaded = true;
                    }

                    // mod.Logger.Log("Loaded non_vr_ui_floatie scene.");

                    GameObject g = GameObject.Find("[NonVRFloatie]");
                    if (g == null)
                        return;
                    Image i = g.GetComponentInChildren<Image>(true);
                    if (i == null)
                        return;

                    floatieNonVR = i.gameObject;

                    // mod.Logger.Log("Found the non VR float.");

                    nonVRFloatieText = floatieNonVR.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (nonVRFloatieText == null)
                        return;
                    // mod.Logger.Log("Found the non VR text.");

                    floatieLine = floatieNonVR.GetComponentInChildren<TutorialLineNonVR>(true);
                    if (floatieLine == null)
                        return;
                    // mod.Logger.Log("Found the non VR line.");

                    ChangeFloatieText = ChangeFloatieTextNonVR;
                    HideFloatie = HideFloatNonVR;
                    ShowFloatie = ShowFloatNonVR;
                    UpdateAttentionTransform = UpdateAttentionTransformNonVR;
                }

                mod.Logger.Log("Floaties have been set up.");
                floatLoaded = true;
            }
            else if (!listenersSetup)
            {
                eyesTransform = PlayerManager.PlayerCamera.transform;
                if (VRManager.IsVREnabled())
                {
                    VRTK_InteractGrab grab = VRTK_DeviceFinder.GetControllerLeftHand(true).transform.GetComponentInChildren<VRTK_InteractGrab>();
                    grab.ControllerGrabInteractableObject += OnItemGrabbedLeftVR;
                    grab.ControllerStartUngrabInteractableObject += OnItemUngrabbedLeftVR;
                    grab = VRTK_DeviceFinder.GetControllerRightHand(true).transform.GetComponentInChildren<VRTK_InteractGrab>();
                    grab.ControllerGrabInteractableObject += OnItemGrabbedRightVR;
                    grab.ControllerStartUngrabInteractableObject += OnItemUngrabbedRightVR;
                }
                else
                {
                    Grabber grab = PlayerManager.PlayerTransform.GetComponentInChildren<Grabber>();
                    grab.Grabbed += OnItemGrabbedRightNonVR;
                    grab.Released += OnItemUngrabbedRightNonVR;
                    SingletonBehaviour<Inventory>.Instance.ItemAddedToInventory += OnItemAddedToInventory;
                }

                mod.Logger.Log("Listeners have been set up.");
                listenersSetup = true;
            }
            else if (!TutorialController.tutorialInProgress)
            {
                // TODO: Make sure TutorialController.tutorialInProgress is false
                if (showing)
                {
                    if (holdingRight == null || holdingRight.State != JobState.Available || !showFloats)
                    {
                        HideFloatie();
                        // Reset job data.
                        jobCars = null;
                        showing = false;
                    }
                    else
                    {
                        timer += delta;
                        if (timer > pointerInterval)
                        {
                            timer -= pointerInterval;
                            // Calculate tracks that cars are on.
                            if (jobCars != null)
                            {
                                consistPointers = new List<TrainCar>();
                                jobConsists = new List<Trainset>();
                                trackNames = new List<TrackID>();
                                locoHooked = false;
                                foreach (Car car in jobCars)
                                    jobCarsUsed[car] = false;
                                foreach (Car car in jobCars)
                                {
                                    if (!jobCarsUsed[car])
                                    {
                                        TrackID track = car.CurrentTrack?.ID; // WTF this is a thing in C#?
                                        TrainCar trainCar;
                                        if (!TrainCar.logicCarToTrainCar.TryGetValue(car, out trainCar))
                                            continue;
                                        Trainset trainset = trainCar.trainset;
                                        foreach (TrainCar tc in trainset.cars)
                                        {
                                            locoHooked = locoHooked || tc.IsLoco;
                                            Car tcl = tc.logicCar;
                                            if (tcl != null)
                                            {
                                                if (jobCarsUsed.ContainsKey(tcl))
                                                    jobCarsUsed[tc.logicCar] = true;
                                                if (track == null)
                                                    track = tcl.CurrentTrack?.ID;
                                            }
                                        }
                                        consistPointers.Add(trainCar);
                                        jobConsists.Add(trainset);
                                        trackNames.Add(track);
                                    }
                                }
                                if (jobConsists.Count > 1)
                                {
                                    counter %= jobConsists.Count;
                                    StringBuilder sb = new StringBuilder(string.Format("The cars for job {0} are currently in {1} different consists on tracks", holdingRight.ID, jobConsists.Count));
                                    for (int i = 0; i < trackNames.Count; i++)
                                    {
                                        if (i > 0 && trackNames.Count > 2)
                                            sb.Append(',');
                                        if (i == trackNames.Count - 1)
                                            sb.Append(" and");
                                        sb.Append(' ');
                                        TrackID t = trackNames[i];
                                        if (i == counter)
                                        {
                                            sb.AppendFormat("<color=#F29839>{0}</color>", t == null ? "(Unknown)" : t.TrackPartOnly);
                                        } else
                                        {
                                            sb.Append(t == null ? "(Unknown)" : t.TrackPartOnly);
                                        }
                                    }
                                    sb.Append('.');
                                    ChangeFloatieText(sb.ToString());
                                    UpdateAttentionTransform(consistPointers[counter].transform);
                                    counter++;
                                } else if (jobConsists.Count == 1)
                                {
                                    TrackID t = trackNames[0];
                                    ChangeFloatieText(string.Format("The cars for job {0} are in the same consist on track <color=#F29839>{1}</color>{2}.", holdingRight.ID, t == null ? "(Unknown)" : t.TrackPartOnly, locoHooked ? " and have a locomotive attached" : ""));
                                    UpdateAttentionTransform(consistPointers[0].transform);
                                } else
                                {
                                    ChangeFloatieText("The job cars could not be found... wtf?");
                                    UpdateAttentionTransform(null);
                                }
                            }
                        }
                    }
                }
                else if (!showing && holdingRight != null && holdingRight.State == JobState.Available && showFloats)
                {
                    // TODO: Read job information and process text accordingly.
                    string overview;
                    if (PlayerJobs.Instance.currentJobs.Count >= LicenseManager.GetNumberOfAllowedConcurrentJobs())
                        overview = "You already have the maximum amount of active jobs.";
                    else if (!LicenseManager.IsLicensedForJob(holdingRight.requiredLicenses))
                        overview = "You don't have the required license(s) for this job.";
                    else if (!CareerManagerDebtController.IsPlayerAllowedToTakeJob())
                        overview = "You still have fees to pay off in the Career Manager.";
                    else
                    {
                        // TODO: Point to first cars of all consists involved.
                        overview = "Searching for corresponding job cars...";
                        switch (holdingRight.jobType)
                        {
                            // if else if else if else if else
                            case JobType.EmptyHaul:
                                jobCars = JobDataExtractor.ExtractEmptyHaulJobData(holdingRight).transportingCars;
                                break;
                            case JobType.ShuntingLoad:
                                jobCars = JobDataExtractor.ExtractShuntingLoadJobData(holdingRight).allCarsToLoad;
                                break;
                            case JobType.ShuntingUnload:
                                jobCars = JobDataExtractor.ExtractShuntingUnloadJobData(holdingRight).allCarsToUnload;
                                break;
                            case JobType.Transport:
                                jobCars = JobDataExtractor.ExtractTransportJobData(holdingRight).transportingCars;
                                break;
                            default:
                                overview = "This job type is unsupported.";
                                break;
                        }
                        if (jobCars != null)
                        {
                            jobCarsUsed = new Dictionary<Car, bool>(jobCars.Count);
                            foreach (Car car in jobCars)
                                jobCarsUsed.Add(car, false);
                        }
                    }
                    ShowFloatie(overview);
                    showing = true;
                    timer = 0;
                }
            }
        }

        // Non VR floating text.
        static void ChangeFloatieTextNonVR(string text)
        {
            nonVRFloatieText.text = text;
        }

        static void HideFloatNonVR()
        {
            floatieNonVR.SetActive(false);
            nonVRFloatieText.text = string.Empty;
            floatieLine.attentionTransform = null;
        }

        static void ShowFloatNonVR(string text)
        {
            HideFloatNonVR();
            nonVRFloatieText.text = text;
            floatieNonVR.SetActive(true);
        }

        static void UpdateAttentionTransformNonVR(Transform attentionTransform)
        {
            // if (floatieNonVR.activeInHierarchy)
            floatieLine.attentionTransform = attentionTransform;
            // TODO: Fiddle with LineRenderer make sure it's working then delete this section.
            LineRenderer lr = floatieLine.GetComponent<LineRenderer>();
            if (lr == null)
            {
                mod.Logger.Log("The non VR LineRenderer is null for some reason.");
            }
            else
            {
                lr.startWidth = startWidth;
                lr.endWidth = endWidth;
                lr.material.mainTexture = pointerTexture;
            }
        }

        // VR floating text.
        static void ChangeFloatieTextVR(string text)
        {
            if (floatieVR != null)
                floatieVR.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
        }

        static void HideFloatVR()
        {
            if (floatieVR != null)
                UnityEngine.Object.Destroy(floatieVR);
        }

        static void ShowFloatVR(string text)
        {
            HideFloatVR();
            if (!string.IsNullOrEmpty(text))
            {
                Vector3 position = eyesTransform.position + eyesTransform.forward * 1.5f;
                Transform parent;
                if (VRManager.IsVREnabled())
                {
                    parent = VRTK_DeviceFinder.PlayAreaTransform();
                }
                else
                {
                    parent = (PlayerManager.Car ? PlayerManager.Car.interior : (SingletonBehaviour<WorldMover>.Exists ? SingletonBehaviour<WorldMover>.Instance.originShiftParent : null));
                }
                floatieVR = (UnityEngine.Object.Instantiate(Resources.Load("tutorial_floatie"), position, Quaternion.identity, parent) as GameObject);
                floatieVR.GetComponent<TutorialFloatie>().UpdateTextExternally(text);
            }
        }

        static void UpdateAttentionTransformVR(Transform attentionTransform)
        {
            if (floatieVR != null)
            {
                floatieVR.GetComponent<Floatie>().attentionPoint = attentionTransform;
                // TODO: Fiddle with LineRenderer make sure it's working then delete this section.
                LineRenderer lr = floatieVR.GetComponent<LineRenderer>();
                if (lr == null)
                {
                    mod.Logger.Log("The VR LineRenderer is null for some reason.");
                }
                else
                {
                    lr.startWidth = startWidth;
                    lr.endWidth = endWidth;
                    lr.material.mainTexture = pointerTexture;
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
                holdingRight = jo.job;
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
            OnItemUngrabbedRight(o.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightNonVR(GameObject o)
        {
            OnItemGrabbedRight(o.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightNonVR(GameObject o)
        {
            OnItemUngrabbedRight(o.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedLeftVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemGrabbedLeft(e.target.GetComponent<InventoryItemSpec>());
        }

        static void OnItemGrabbedRightVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemGrabbedRight(e.target.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedLeftVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemUngrabbedLeft(e.target.GetComponent<InventoryItemSpec>());
        }

        static void OnItemUngrabbedRightVR(object sender, ObjectInteractEventArgs e)
        {
            OnItemUngrabbedRight(e.target.GetComponent<InventoryItemSpec>());
        }
    }

    [HarmonyPatch(typeof(TutorialController), "ShowFloatieVR")]
    class TutorialController_ShowFloatieVR_Patch
    {
        static void Prefix(TutorialController __instance)
        {
            Debug.Log(string.Format("[TestFloatMod] A TutorialController is trying to ShowFloatieVR() with an instance of tutorialFloatie named: {0} from scene: {1}", __instance.tutorialFloatie.name, __instance.tutorialFloatie.scene.name));

        }
    }
}
