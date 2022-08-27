using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV.Logic.Job;
using DV.ServicePenalty;
using DVDispatcherMod.Extensions;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHints {
    public class JobDispatch {
        private readonly Job _job;
        private readonly IdGenerator _idGenerator;
        private readonly CareerManagerDebtController _careerManagerDebtController;
        private readonly PlayerJobs _playerJobs;

        public JobDispatch(Job job) {
            _job = job;

            _idGenerator = SingletonBehaviour<IdGenerator>.Instance ?? throw new InvalidOperationException("IdGenerator singleton is null");
            _careerManagerDebtController = SingletonBehaviour<CareerManagerDebtController>.Instance ?? throw new InvalidOperationException("CareerManagerDebtController singleton is null");
            _playerJobs = PlayerJobs.Instance ?? throw new InvalidOperationException("PlayerJobs.Instance is nul");
        }

        public DispatcherHint GetDispatcherHint(int highlightIndex) {
            if (_job.State == JobState.Completed) {
                return new DispatcherHint("This job is completed.");
            } else if (_job.State == JobState.Abandoned) {
                return new DispatcherHint("This job is abandoned.");
            } else if (_job.State == JobState.Failed) {
                return new DispatcherHint("This job is failed.");
            } else if (_job.State == JobState.Expired) {
                return new DispatcherHint("This job is expired.");
            }

            if (_job.State == JobState.Available) {
                var jobNotAllowedText = GetJobNotAllowedTextOrNull();

                if (jobNotAllowedText != null) {
                    return new DispatcherHint(jobNotAllowedText);
                }
            }

            var firstUnfinishedTasks = GetFirstUnfinishedTasks(_job.tasks.First());
            if (!firstUnfinishedTasks.Any()) {
                return new DispatcherHint("The job is probably complete. Try turning it in.");
            }

            var dispatchTrainSets = GetDispatchTrainSets(firstUnfinishedTasks);

            if (!dispatchTrainSets.Any()) {
                return new DispatcherHint("The job cars could not be found... wtf?");
            }

            return GetDispatcherHintFromDispatchTrainSets(dispatchTrainSets, highlightIndex);
        }

        private string GetJobNotAllowedTextOrNull() {
            if (_playerJobs.currentJobs.Count >= LicenseManager.GetNumberOfAllowedConcurrentJobs()) {
                return "You already have the maximum number of active jobs.";
            } else if (!LicenseManager.IsLicensedForJob(_job.requiredLicenses)) {
                return "You don't have the required license(s) for this job.";
            } else if (!_careerManagerDebtController.IsPlayerAllowedToTakeJob()) {
                return "You still have fees to pay off in the Career Manager.";
            } else {
                return null;
            }
        }

        private DispatcherHint GetDispatcherHintFromDispatchTrainSets(List<DispatchTrainSet> dispatchTrainSets, int highlightIndex) {
            GetHighlightedTrainSetAndCarGroupIndex(highlightIndex, dispatchTrainSets, out var highlightTrainSetIndex, out var highlightCarGroupIndex);

            var dispatcherHintText = GetDispatcherHintTextForAtLeastOneTrainSet(dispatchTrainSets, highlightTrainSetIndex);

            var taskOverview = TaskOverviewGenerator.GetTaskOverview(_job);

            var floatieText = dispatcherHintText + Environment.NewLine + taskOverview;

            var attentionTransform = dispatchTrainSets[highlightTrainSetIndex].CarGroupTransforms[highlightCarGroupIndex];

            return new DispatcherHint(floatieText, attentionTransform);
        }

        private List<DispatchTrainSet> GetDispatchTrainSets(List<Task> carRelevantTasks) {
            var tasks = carRelevantTasks;

            var jobCars = tasks.SelectMany(t => t.GetTaskData().cars).ToList();

            var cars = jobCars.Select(TryGetTrainCarFromJobCar).Where(c => c != null).ToList();

            var dispatchTrainSets =
                cars.GroupBy(c => c.trainset)
                    .Select(g => new DispatchTrainSet(
                        GetNamedTrackIDFromPreferredCarsOrTrainSet(g.ToList(), g.Key),
                        IsAnyLocoInConsist(g.Key),
                        GroupAdjacentCars(g).Select(GetTransformFromAdjacentCars).ToList())
                    ).ToList();

            return dispatchTrainSets;
        }

        private TrainCar TryGetTrainCarFromJobCar(Car car) {
            if (_idGenerator.logicCarToTrainCar.TryGetValue(car, out var trainCar)) {
                return trainCar;
            } else {
                return null;
            }
        }

        private static string GetNamedTrackIDFromPreferredCarsOrTrainSet(List<TrainCar> preferredCars, Trainset trainSet) {
            var preferredCarsTrackID = preferredCars.Select(c => c.logicCar.CurrentTrack?.ID).Where(id => id != null && !id.IsGeneric()).Select(id => id.TrackPartOnly).WhereNotNull().FirstOrDefault();
            if (preferredCarsTrackID != null) {
                return preferredCarsTrackID;
            }

            return trainSet.cars.Select(c => c.logicCar.CurrentTrack?.ID).WhereNotNull().Where(id => !id.IsGeneric()).Select(id => id.TrackPartOnly).WhereNotNull().FirstOrDefault();
        }

        private static bool IsAnyLocoInConsist(Trainset trainset) {
            return trainset.cars.Any(c => c.IsLoco);
        }

        private static List<List<TrainCar>> GroupAdjacentCars(IEnumerable<TrainCar> trainCars) {
            return trainCars.OrderBy(c => c.indexInTrainset).Aggregate(new List<List<TrainCar>>(), AddCarToAdjacencyAggregatedCars);
        }

        private static List<List<TrainCar>> AddCarToAdjacencyAggregatedCars(List<List<TrainCar>> carGroups, TrainCar trainCar) {
            if (carGroups.Any()) {
                var lastCarGrop = carGroups.Last();
                var lastTrainCar = lastCarGrop.Last();
                if (lastTrainCar.indexInTrainset + 1 == trainCar.indexInTrainset) {
                    lastCarGrop.Add(trainCar);
                    return carGroups;
                }
            }

            carGroups.Add(new List<TrainCar> { trainCar });

            return carGroups;
        }

        private static Transform GetTransformFromAdjacentCars(List<TrainCar> cars) {
            // it would be nice if we could point to the middle of the 2 center cars in case the number of cars is even
            return cars[cars.Count / 2].transform;
        }

        /// <summary>
        /// Figure out the current tasks.
        /// </summary>
        /// <param name="startingTask"></param>
        /// <returns></returns>
        private List<Task> GetFirstUnfinishedTasks(Task startingTask) {
            var toReturn = new List<Task>();
            if (startingTask != null && startingTask.state == TaskState.InProgress && startingTask.Job == _job) {
                var tasks = startingTask.GetTaskData().nestedTasks;
                switch (startingTask.InstanceTaskType) {
                    case TaskType.Parallel:
                        foreach (var t in tasks) {
                            toReturn.AddRange(GetFirstUnfinishedTasks(t));
                        }
                        break;
                    case TaskType.Sequential:
                        foreach (var t in tasks) {
                            if (t.state == TaskState.InProgress) {
                                toReturn.AddRange(GetFirstUnfinishedTasks(t));
                                break;
                            }
                        }
                        break;
                    default:
                        toReturn.Add(startingTask);
                        break;
                }
            }
            return toReturn;
        }

        private string GetDispatcherHintTextForAtLeastOneTrainSet(List<DispatchTrainSet> dispatchTrainSets, int highlightTrainSetIndex) {
            if (dispatchTrainSets.Count == 1) {
                var dispatchTrainSet = dispatchTrainSets[0];
                var sb = new StringBuilder($"The cars for job {_job.ID} are in the same consist");
                var carGroupCount = dispatchTrainSet.CarGroupTransforms.Count;
                if (carGroupCount > 1) {
                    sb.Append($" (but in {carGroupCount} distinct car groups)");
                }
                if (dispatchTrainSet.TrackIDOrNull != null) {
                    sb.Append($" on track <color=#F29839>{dispatchTrainSet.TrackIDOrNull}</color>");
                }
                if (dispatchTrainSet.HasLocoHooked) {
                    sb.Append(" and have a locomotive attached");
                }
                sb.Append(".");
                return sb.ToString();
            } else {
                var sb = new StringBuilder($"The cars for job {_job.ID} are currently in {dispatchTrainSets.Count} different consists");

                var namedTracksWithHighlights = dispatchTrainSets.Select((s, i) => new { s.TrackIDOrNull, IsHighlighted = (i == highlightTrainSetIndex) }).Where(t => t.TrackIDOrNull != null).GroupBy(t => t.TrackIDOrNull, (key, values) => new { Track = key, IsHighlighted = values.Any(t => t.IsHighlighted) }).ToList();

                if (namedTracksWithHighlights.Any()) {
                    // there is at least one track with a meaningful name
                    if (dispatchTrainSets.Any(d => d.TrackIDOrNull == null)) {
                        sb.Append(", some");
                    }

                    if (namedTracksWithHighlights.Count == 1) {
                        sb.Append(" on track ");
                    } else {
                        sb.Append(" on tracks ");
                    }
                    for (var i = 0; i < namedTracksWithHighlights.Count; i++) {
                        if (namedTracksWithHighlights[i].IsHighlighted) {
                            sb.Append("<color=#F29839>");
                            sb.Append(namedTracksWithHighlights[i].Track);
                            sb.Append("</color>");
                        } else {
                            sb.Append(namedTracksWithHighlights[i].Track);
                        }
                        if (i < namedTracksWithHighlights.Count - 2) {
                            sb.Append(", ");
                        } else if (i < namedTracksWithHighlights.Count - 1) {
                            sb.Append(" and ");
                        }
                    }
                }

                sb.Append('.');
                return sb.ToString();
            }
        }

        private void GetHighlightedTrainSetAndCarGroupIndex(int unboundedHighlightIndex, List<DispatchTrainSet> dispatchTrainSets, out int trainSetIndex, out int carGroupIndex) {
            var highlightIndex = unboundedHighlightIndex % dispatchTrainSets.Sum(t => t.CarGroupTransforms.Count);

            int currentIndex = 0;
            for (var currentTrainSetIndex = 0; currentTrainSetIndex < dispatchTrainSets.Count; currentTrainSetIndex += 1) {
                var currentTrainSet = dispatchTrainSets[currentTrainSetIndex];
                for (var currentCarGroupIndex = 0; currentCarGroupIndex < currentTrainSet.CarGroupTransforms.Count; currentCarGroupIndex += 1) {
                    if (currentIndex == highlightIndex) {
                        trainSetIndex = currentTrainSetIndex;
                        carGroupIndex = currentCarGroupIndex;
                        return;
                    }

                    currentIndex++;
                }
            }

            throw new InvalidOperationException("could not determine train set and car group index");
        }

        private class DispatchTrainSet {
            public DispatchTrainSet(string trackIDOrNull, bool hasLocoHooked, List<Transform> carGroupTransforms) {
                TrackIDOrNull = trackIDOrNull;
                HasLocoHooked = hasLocoHooked;
                CarGroupTransforms = carGroupTransforms;
            }

            public string TrackIDOrNull { get; }
            public bool HasLocoHooked { get; }
            public List<Transform> CarGroupTransforms { get; }
        }
    }
}