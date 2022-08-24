using System;
using DV.Logic.Job;
using VRTK;

namespace DVDispatcherMod.PlayerInteractionManagers {
    internal class VRPlayerInteractionManager : IPlayerInteractionManager {
        public VRPlayerInteractionManager(VRTK_InteractGrab interactGrab) {
            interactGrab.ControllerGrabInteractableObject += HandleControllerGrabInteractableObject;
            interactGrab.ControllerStartUngrabInteractableObject += HandleControllerStartUngrabInteractableObject;
        }

        public Job JobOfInterest { get; private set; }

        public event Action JobOfInterestChanged;

        private void HandleControllerGrabInteractableObject(object sender, ObjectInteractEventArgs e) {
            var inventoryItemSpec = e.target?.GetComponent<InventoryItemSpec>();
            if (inventoryItemSpec != null) {
                var job = TryGetJobFromInventoryItemSpec(inventoryItemSpec);
                if (job != null) {
                    JobOfInterest = job;
                    JobOfInterestChanged?.Invoke();
                }
            }
        }

        private void HandleControllerStartUngrabInteractableObject(object sender, ObjectInteractEventArgs e) {
            if (JobOfInterest != null) {
                JobOfInterest = null;
                JobOfInterestChanged?.Invoke();
            }
        }


        private static Job TryGetJobFromInventoryItemSpec(InventoryItemSpec inventoryItemSpec) {
            if (inventoryItemSpec.GetComponent<JobOverview>() is JobOverview jo) {
                return jo.job;
            }
            if (inventoryItemSpec.GetComponent<JobBooklet>() is JobBooklet jb) {
                return jb.job;
            }
            return null;
        }
    }
}