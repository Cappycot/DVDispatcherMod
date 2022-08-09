using System;
using DV.Logic.Job;
using UnityEngine;

namespace DVDispatcherMod.PlayerInteractionManagers {
    public class NonVRPlayerInteractionManager : IPlayerInteractionManager {
        private readonly Grabber _grabber;
        private Job _grabbedJob;
        private Job _hoveredJob;

        public NonVRPlayerInteractionManager(Grabber grabber, Inventory inventory) {
            _grabber = grabber;
            grabber.Grabbed += HandleGrabbed;
            grabber.Released += _ => HandleHeldItemReleased();
            grabber.Hovered += HandleHovered;
            grabber.Unhovered += _ => HandleUnhovered();
            inventory.ItemAddedToInventory += (go, i) => HandleHeldItemReleased();
        }

        public Job JobOfInterest => _grabbedJob ?? _hoveredJob;

        public event Action JobOfInterestChanged;

        private void HandleGrabbed(GameObject gameObject) {
            var inventoryItemSpec = gameObject?.GetComponent<InventoryItemSpec>();
            if (inventoryItemSpec != null) {
                var job = GetJobFromInventoryItemSpec(inventoryItemSpec);
                if (job != null) {
                    _grabbedJob = job;
                    JobOfInterestChanged?.Invoke();
                }
            }
        }

        private void HandleHeldItemReleased() {
            _grabbedJob = null;
            JobOfInterestChanged?.Invoke();
        }

        private void HandleHovered(GameObject gameObject) {
            if (gameObject != null) {
                var job = GetJobFromGameObject(gameObject);
                if (job != null) {
                    _hoveredJob = job;
                    JobOfInterestChanged?.Invoke();
                }
            }
        }

        private void HandleUnhovered() {
            _hoveredJob = null;
            JobOfInterestChanged?.Invoke();
        }

        private static Job GetJobFromInventoryItemSpec(InventoryItemSpec inventoryItemSpec) {
            if (inventoryItemSpec.GetComponent<JobOverview>() is JobOverview jo) {
                return jo.job;
            }
            if (inventoryItemSpec.GetComponent<JobBooklet>() is JobBooklet jb) {
                return jb.job;
            }
            return null;
        }

        private static Job GetJobFromGameObject(GameObject gameObject) {
            if (gameObject.GetComponent<JobOverview>() is JobOverview jo) {
                return jo.job;
            }
            if (gameObject.GetComponent<JobBooklet>() is JobBooklet jb) {
                return jb.job;
            }
            return null;
        }
    }
}