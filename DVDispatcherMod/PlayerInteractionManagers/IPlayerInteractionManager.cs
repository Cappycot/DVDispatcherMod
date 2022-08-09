using System;
using DV.Logic.Job;

namespace DVDispatcherMod.PlayerInteractionManagers {
    public interface IPlayerInteractionManager {
        Job JobOfInterest { get; }
        event Action JobOfInterestChanged;
    }
}