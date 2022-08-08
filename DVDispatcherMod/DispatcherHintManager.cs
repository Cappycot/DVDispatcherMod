using JetBrains.Annotations;

namespace DVDispatcherMod {
    public static class NonVRDispatchHintManagerFactory {
        private static NonVRDispatcherHintShower _dispatcherHintShower;
        private static IPlayerInteractionManager _playerInteractionManager;

        public static DispatcherHintManager TryCreate() {
            if (LoadingScreenManager.IsLoading || !WorldStreamingInit.IsLoaded || !SingletonBehaviour<Inventory>.Exists) {
                return null;
            }

            _dispatcherHintShower = _dispatcherHintShower ?? NonVRDispatcherHintShowerFactory.TryCreate();
            if (_dispatcherHintShower == null) {
                return null;
            }

            _playerInteractionManager = _playerInteractionManager ?? NonVRPlayerInteractionManagerFactory.TryCreate();
            if (_playerInteractionManager == null) {
                return null;
            }

            return new DispatcherHintManager(_playerInteractionManager, _dispatcherHintShower);
        }
    }

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