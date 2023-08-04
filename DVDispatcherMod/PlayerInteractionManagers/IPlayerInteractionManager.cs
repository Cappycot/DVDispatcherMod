using System;
using DV.Logic.Job;

namespace DVDispatcherMod.PlayerInteractionManagers {
    public interface IPlayerInteractionManager : IDisposable {
        Job JobOfInterest { get; }
        event Action JobOfInterestChanged;
    }
}