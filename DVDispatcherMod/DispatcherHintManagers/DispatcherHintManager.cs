using System;
using DVDispatcherMod.DispatcherHints;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using JetBrains.Annotations;

namespace DVDispatcherMod.DispatcherHintManagers {
    public sealed class DispatcherHintManager : IDisposable {
        private readonly IPlayerInteractionManager _playerInteractionManager;
        private readonly IDispatcherHintShower _dispatcherHintShower;
        private readonly TaskOverviewGenerator _taskOverviewGenerator;

        private int _counterValue;

        public DispatcherHintManager([NotNull] IPlayerInteractionManager playerInteractionManager, [NotNull] IDispatcherHintShower dispatcherHintShower, TaskOverviewGenerator taskOverviewGenerator) {
            _playerInteractionManager = playerInteractionManager;
            _dispatcherHintShower = dispatcherHintShower;
            _taskOverviewGenerator = taskOverviewGenerator;

            _playerInteractionManager.JobOfInterestChanged += HandleJobObInterestChanged;
        }

        public void SetCounter(int counterValue) {
            _counterValue = counterValue;
            UpdateDispatcherHint();
        }

        private void HandleJobObInterestChanged() {
            UpdateDispatcherHint();

            // uncomment this to spam the log with outputs of the job and task tree
            //var job = _playerInteractionManager.JobOfInterest;
            //if (job != null) {
            //    DebugOutputJobWriter.DebugOutputJob(job);
            //}
        }

        private void UpdateDispatcherHint() {
            var currentHint = GetCurrentDispatcherHint();
            _dispatcherHintShower.SetDispatcherHint(currentHint);
        }

        private DispatcherHint GetCurrentDispatcherHint() {
            var job = _playerInteractionManager.JobOfInterest;
            if (job != null) {
                return new JobDispatch(job, _taskOverviewGenerator).GetDispatcherHint(_counterValue);
            } else {
                return null;
            }
        }

        public void Dispose() {
            _dispatcherHintShower.SetDispatcherHint(null);
            _playerInteractionManager?.Dispose();
        }
    }
}