using System.Linq;
using Harmony12;
using System.Reflection;
using DV.Logic.Job;
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
        private static JobDispatch _hovered;

        static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            _timer += delta;

            if (!_listenersSetup) {
                // eyesTransform = PlayerManager.PlayerCamera.transform;
                if (VRManager.IsVREnabled()) {
                    if (_dispatchHintShower == null) {
                        _dispatchHintShower = new VRDispatchHintShower();
                    }

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
                    grab.Hovered += OnItemHovered;
                    grab.Unhovered += OnItemUnhovered;
                    SingletonBehaviour<Inventory>.Instance.ItemAddedToInventory += OnItemAddedToInventory;
                }

                mod.Logger.Log(string.Format("Floaties have been set up, total time elapsed: {0:0.00} seconds.", _timer));
                _listenersSetup = true;
            } else {
                if (_timer > POINTER_INTERVAL) {
                    _counter++;
                    _timer %= POINTER_INTERVAL;

                    UpdateDispatcherHint();
                }
            }
        }

        private static void OnItemHovered(GameObject obj) {
            Job job = null;
            var jo = obj.GetComponent<JobOverview>();
            if (jo != null) {
                job = jo.job;
            } else {
                var jb = obj.GetComponent<JobBooklet>();
                if (jb != null) {
                    job = jb.job;
                }
            }
            if (job != null) {
                _hovered = new JobDispatch(job);
                UpdateDispatcherHint();
            }
        }

        private static void OnItemUnhovered(GameObject obj) {
            _hovered = null;
            UpdateDispatcherHint();
        }

        private static void UpdateDispatcherHint() {
            var currentHint = GetCurrentDispatcherHint();
            _dispatchHintShower.SetDispatcherHint(currentHint);
        }

        private static DispatcherHint GetCurrentDispatcherHint() {
            if (!_isEnabled) {
                return null;
            }

            if (_holdingRight != null) {
                return _holdingRight.GetDispatcherHint(_counter);
            } else if (_hovered != null) {
                return _hovered.GetDispatcherHint(_counter);
            } else {
                return null;
            }
        }

        static void OnItemGrabbedRight(InventoryItemSpec iis) {
            if (iis == null) {
                return;
            }

            var job = TryGetJobFromComponent(iis);

            if (job != null) {
                DebugOutputJob(job);

                _holdingRight = new JobDispatch(job);
            }

            _counter = 0;
            _timer = 0;

            UpdateDispatcherHint();
        }

        private static Job TryGetJobFromComponent(Component iis) {
            var jo = iis.GetComponent<JobOverview>();
            Job job = null;
            if (jo != null) {
                job = jo.job;
            } else {
                var jb = iis.GetComponent<JobBooklet>();
                if (jb != null) {
                    job = jb.job;
                }
            }
            return job;
        }

        private static void DebugOutputJob(Job job) {
            DebugLogIndented(0, job.GetType().Name, job.ID);
            DebugLogIndented(1, "jobType", job.jobType);
            DebugLogIndented(1, "State", job.State);
            DebugLogIndented(1, "chainData");
            DebugLogIndented(2, "chainOriginYardId", job.chainData.chainOriginYardId);
            DebugLogIndented(2, "chainDestinationYardId", job.chainData.chainDestinationYardId);
            DebugLogIndented(1, "tasks");
            foreach (var jobTask in job.tasks) {
                DebugOutputTask(2, jobTask);
            }
        }

        private static void DebugOutputTask(int indent, Task jobTask) {
            DebugLogIndented(indent, jobTask.GetType().Name);
            DebugLogIndented(indent + 1, "InstanceTaskType", jobTask.InstanceTaskType);
            DebugLogIndented(indent + 1, "state", jobTask.state);

            var taskData = jobTask.GetTaskData();
            if (taskData.cars != null) {
                DebugLogIndented(indent + 1, "cars", string.Join(", ", taskData.cars.Select(c => c.ID)));
            }
            DebugLogIndented(indent + 1, "startTrack", taskData.startTrack?.ID?.FullDisplayID);
            DebugLogIndented(indent + 1, "destinationTrack", taskData.destinationTrack?.ID?.FullDisplayID);
            DebugLogIndented(indent + 1, "warehouseTaskType", taskData.warehouseTaskType);

            if (taskData.nestedTasks != null) {
                if (taskData.nestedTasks.Any()) {
                    DebugLogIndented(indent + 1, "nestedTasks (" + taskData.nestedTasks.Count + ")");

                    foreach (var nestedTask in taskData.nestedTasks) {
                        DebugOutputTask(indent + 2, nestedTask);
                    }
                }
            }
        }

        private static void DebugLogIndented(int indent, string name, object value = null) {
            var content = value != null ? (name + ": " + value) : name;
            ModEntry.Logger.Log(string.Join("", Enumerable.Repeat("    ", indent)) + content);
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
