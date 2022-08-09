using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DV.Logic.Job;
using DV.ServicePenalty;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHints {
    public class JobDispatch {
        private readonly Job _job;

        public JobDispatch(Job job) {
            _job = job;
        }

        public DispatcherHint GetDispatcherHint(int highlightIndex) {
            if (_job.State == JobState.Completed) {
                return new DispatcherHint("This job is completed.", null);
            } else if (_job.State == JobState.Abandoned) {
                return new DispatcherHint("This job is abandoned.", null);
            } else if (_job.State == JobState.Failed) {
                return new DispatcherHint("This job is failed.", null);
            } else if (_job.State == JobState.Expired) {
                return new DispatcherHint("This job is expired.", null);
            }

            if (_job.State == JobState.Available) {
                var jobNotAllowedText = GetJobNotAllowedTextOrNull();

                if (jobNotAllowedText != null) {
                    return new DispatcherHint(jobNotAllowedText, null);
                }
            }

            var firstUnfinishedTasks = GetFirstUnfinishedTasks(_job.tasks.First());

            var carGroupOnTracks = GetCarGroupsOnTracks(firstUnfinishedTasks);

            var dispatcherHintText = GetDispatcherHintText(carGroupOnTracks, firstUnfinishedTasks.Any(), highlightIndex);

            var taskOverview = TaskOverviewGenerator.GetTaskOverview(_job);

            var attentionTransform = GetPointerAt(carGroupOnTracks, highlightIndex);

            var floatieText = dispatcherHintText + Environment.NewLine + taskOverview;

            return new DispatcherHint(floatieText, attentionTransform);
        }

        private string GetJobNotAllowedTextOrNull() {
            if (PlayerJobs.Instance.currentJobs.Count >= LicenseManager.GetNumberOfAllowedConcurrentJobs()) {
                return "You already have the maximum number of active jobs.";
            } else if (!LicenseManager.IsLicensedForJob(_job.requiredLicenses)) {
                return "You don't have the required license(s) for this job.";
            } else if (!SingletonBehaviour<CareerManagerDebtController>.Instance.IsPlayerAllowedToTakeJob()) {
                return "You still have fees to pay off in the Career Manager.";
            } else {
                return null;
            }
        }

        private static List<CarGroupOnTrack> GetCarGroupsOnTracks(List<Task> carRelevantTasks) {
            var tasks = carRelevantTasks;

            var jobCars = tasks.SelectMany(t => t.GetTaskData().cars).ToList();

            var carGroupsOnTracks =
                from car in jobCars
                let trainCar = TryGetTrainCarFromJobCar(car)
                where trainCar != null
                group car by trainCar.trainset
                into trainSetWithCars
                let trackID = trainSetWithCars.Select(c => c.CurrentTrack?.ID?.TrackPartOnly).FirstOrDefault(id => id != null)
                let hasLocoHooked = (PlayerManager.LastLoco?.trainset == trainSetWithCars.Key)
                let consistTransform = trainSetWithCars.Key.cars[trainSetWithCars.Key.cars.Count / 2].transform
                select new CarGroupOnTrack(trackID, hasLocoHooked, consistTransform);

            return carGroupsOnTracks.ToList();
        }

        private static TrainCar TryGetTrainCarFromJobCar(Car car) {
            if (SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(car, out var trainCar)) {
                return trainCar;
            } else {
                return null;
            }
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

        private string GetDispatcherHintText(List<CarGroupOnTrack> carGroupsOnTracks, bool hasAnyUnfinishedTask, int highlightIndex) {
            if (carGroupsOnTracks.Count > 0) {
                return GetDispatcherHintTextForAtLeastOneCarGroup(carGroupsOnTracks, highlightIndex);
            } else if (!hasAnyUnfinishedTask) {
                return "The job is probably complete. Try turning it in.";
            } else {
                return "The job cars could not be found... wtf?";
            }
        }

        private string GetDispatcherHintTextForAtLeastOneCarGroup(List<CarGroupOnTrack> carGroupsOnTracks, int highlightIndex) {
            if (carGroupsOnTracks.Count == 1) {
                var carGroupOnTrack = carGroupsOnTracks[0];
                return $"The cars for job {_job.ID} are in the same consist on track <color=#F29839>{(carGroupOnTrack == null ? "(Unknown)" : carGroupOnTrack.TrackID)}</color>{(carGroupOnTrack.HasLastPlayerLocoHooked ? " and have a locomotive attached" : "")}.";
            } else {
                highlightIndex %= carGroupsOnTracks.Count;
                var sb = new StringBuilder($"The cars for job {_job.ID} are currently in {carGroupsOnTracks.Count} different consists on tracks");
                for (var i = 0; i < carGroupsOnTracks.Count; i++) {
                    if (i > 0 && carGroupsOnTracks.Count > 2) {
                        sb.Append(',');
                    }
                    if (i == carGroupsOnTracks.Count - 1) {
                        sb.Append(" and");
                    }
                    sb.Append(' ');
                    var carGroupOnTrack = carGroupsOnTracks[i];
                    if (i == highlightIndex) {
                        sb.AppendFormat("<color=#F29839>{0}</color>", carGroupOnTrack == null ? "(Unknown)" : carGroupOnTrack.TrackID);
                    } else {
                        sb.Append(carGroupOnTrack == null ? "(Unknown)" : carGroupOnTrack.TrackID);
                    }
                }
                sb.Append('.');
                return sb.ToString();
            }
        }

        private static Transform GetPointerAt(List<CarGroupOnTrack> carGroupsOnTracks, int index) {
            if (!carGroupsOnTracks.Any()) {
                return null;
            }

            index %= carGroupsOnTracks.Count;
            var carGroupOnTrack = carGroupsOnTracks[index];
            if (carGroupOnTrack.HasLastPlayerLocoHooked) {
                return PlayerManager.LastLoco?.transform;
            }

            // TODO: Raise pointer to middle of car height.
            return carGroupOnTrack.ConsistTransform;
        }

        private class CarGroupOnTrack {
            public string TrackID { get; }
            public bool HasLastPlayerLocoHooked { get; }
            public Transform ConsistTransform { get; }

            public CarGroupOnTrack(string trackID, bool hasLastPlayerLocoHooked, Transform consistTransform) {
                TrackID = trackID;
                HasLastPlayerLocoHooked = hasLastPlayerLocoHooked;
                ConsistTransform = consistTransform;
            }
        }
    }
}
