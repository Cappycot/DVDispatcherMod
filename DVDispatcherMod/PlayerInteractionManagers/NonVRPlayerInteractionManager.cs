using System;
using DV.Interaction;
using DV.Logic.Job;
using UnityEngine;

namespace DVDispatcherMod.PlayerInteractionManagers {
    public sealed class NonVRPlayerInteractionManager : IPlayerInteractionManager {
        private readonly Grabber _grabber;
        private Job _grabbedJob;
        private Job _hoveredJob;

        public NonVRPlayerInteractionManager(Grabber grabber) {
            _grabber = grabber;

            _grabber.GrabStarted += HandleGrabbed;
            _grabber.GrabStopped += HandleHeldItemReleased;
            _grabber.Raycaster.Hovered += HandleHovered;
            _grabber.Raycaster.UnHovered += HandleUnhovered;
        }

        public Job JobOfInterest => _grabbedJob ?? _hoveredJob;

        public event Action JobOfInterestChanged;

        private void HandleGrabbed(AGrabHandler grabHandler) {
            var gameObject = GetGameObject(grabHandler);
            var inventoryItemSpec = gameObject?.GetComponent<InventoryItemSpec>();
            if (inventoryItemSpec != null) {
                var job = TryGetJobFromInventoryItemSpec(inventoryItemSpec);
                if (job != null) {
                    _grabbedJob = job;
                    JobOfInterestChanged?.Invoke();
                }
            }
        }

        private void HandleHeldItemReleased(AGrabHandler grabHandler) {
            _grabbedJob = null;
            JobOfInterestChanged?.Invoke();
        }

        private void HandleHovered(AGrabHandler grabHandler) {
            var gameObject = GetGameObject(grabHandler);
            if (gameObject != null) {
                var job = TryGetJobFromGameObject(gameObject);
                if (job != null) {
                    _hoveredJob = job;
                    JobOfInterestChanged?.Invoke();
                }
            }
        }

        private void HandleUnhovered(AGrabHandler aGrabHandler) {
            _hoveredJob = null;
            JobOfInterestChanged?.Invoke();
        }

        private static GameObject GetGameObject(AGrabHandler grabHandler) {
            return grabHandler.GetComponent<InventoryItemSpec>()?.gameObject;
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

        private static Job TryGetJobFromGameObject(GameObject gameObject) {
            if (gameObject.GetComponent<JobOverview>() is JobOverview jo) {
                return jo.job;
            }
            if (gameObject.GetComponent<JobBooklet>() is JobBooklet jb) {
                return jb.job;
            }
            return null;
        }

        public void Dispose() {
            _grabber.GrabStarted += HandleGrabbed;
            _grabber.GrabStopped += HandleHeldItemReleased;
            _grabber.Raycaster.Hovered += HandleHovered;
            _grabber.Raycaster.UnHovered += HandleUnhovered;
        }
    }
}