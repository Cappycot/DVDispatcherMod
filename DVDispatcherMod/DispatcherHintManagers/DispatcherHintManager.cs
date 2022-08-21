using DVDispatcherMod.DispatcherHints;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using JetBrains.Annotations;

namespace DVDispatcherMod.DispatcherHintManagers {
    public class DispatcherHintManager {
        private readonly IPlayerInteractionManager _playerInteractionManager;
        private readonly IDispatcherHintShower _dispatcherHintShower;

        private bool _isEnabled = true;
        private int _counterValue;

        public DispatcherHintManager([NotNull] IPlayerInteractionManager playerInteractionManager, [NotNull] IDispatcherHintShower dispatcherHintShower) {
            _playerInteractionManager = playerInteractionManager;
            _dispatcherHintShower = dispatcherHintShower;

            _playerInteractionManager.JobOfInterestChanged += HandleJobObInterestChanged;
        }

        public void SetIsEnabled(bool value) {
            _isEnabled = value;
            UpdateDispatcherHint();
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
            if (!_isEnabled) {
                return null;
            }

            var job = _playerInteractionManager.JobOfInterest;
            if (job != null) {
                return new JobDispatch(job).GetDispatcherHint(_counterValue);
            } else {
                return null;
            }
        }
    }
}