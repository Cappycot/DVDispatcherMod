using DV.Logic.Job;
using DV.ServicePenalty;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DVDispatcherMod {
    public class JobDispatch {
        public Job Job { get; }

        private bool _jobNotAllowed;
        private string _jobNotAllowedText;

        private List<Car> _jobCars;
        private Dictionary<Car, bool> _jobCarsUsed;
        private List<Trainset> _jobConsists; // Important consists to point at.
        private int _currentTasks;
        private List<TrackID> _currentTracks;

        public JobDispatch(Job job) {
            Job = job;
            _jobCars = new List<Car>();
            _jobCarsUsed = new Dictionary<Car, bool>();
            _jobConsists = new List<Trainset>();
            _currentTasks = 0;
            _currentTracks = new List<TrackID>();
            UpdateJobCars();
            UpdateJobPrivilege();
        }

        public void UpdateJobCars() {
            _jobCars.Clear();
            _jobCarsUsed.Clear();
            _jobConsists.Clear();
            _currentTracks.Clear();
            var tasks = GetFirstUnfinishedTasks();
            _currentTasks = tasks.Count;
            foreach (var t in tasks)
                _jobCars.AddRange(t.GetTaskData().cars);
            foreach (var c in _jobCars)
                _jobCarsUsed[c] = false;
            foreach (var car in _jobCars) {
                if (!_jobCarsUsed[car]) {
                    var trackID = car.CurrentTrack?.ID;
                    if (!SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(car, out var trainCar))
                        continue;

                    var trainset = trainCar.trainset;
                    foreach (var tc in trainset.cars) {
                        var c = tc.logicCar;
                        if (c != null) {
                            if (_jobCarsUsed.ContainsKey(c))
                                _jobCarsUsed[c] = true;
                            if (trackID == null)
                                trackID = c.CurrentTrack?.ID;
                        }
                    }
                    _jobConsists.Add(trainset);
                    _currentTracks.Add(trackID);
                }
            }
        }

        // TODO: Find different place to define and call?
        public void UpdateJobPrivilege() {
            _jobNotAllowed = false;
            if (PlayerJobs.Instance.currentJobs.Count >= LicenseManager.GetNumberOfAllowedConcurrentJobs()) {
                _jobNotAllowed = true;
                _jobNotAllowedText = "You already have the maximum number of active jobs.";
            } else if (!LicenseManager.IsLicensedForJob(Job.requiredLicenses)) {
                _jobNotAllowed = true;
                _jobNotAllowedText = "You don't have the required license(s) for this job.";
            } else if (!SingletonBehaviour<CareerManagerDebtController>.Instance.IsPlayerAllowedToTakeJob()) {
                _jobNotAllowed = true;
                _jobNotAllowedText = "You still have fees to pay off in the Career Manager.";
            }
        }

        private List<Task> GetFirstUnfinishedTasks() {
            return GetFirstUnfinishedTasks(Job.tasks.First());
        }

        /// <summary>
        /// Figure out the current tasks.
        /// </summary>
        /// <param name="startingTask"></param>
        /// <returns></returns>
        private List<Task> GetFirstUnfinishedTasks(Task startingTask) {
            var toReturn = new List<Task>();
            if (startingTask != null && startingTask.state == TaskState.InProgress && startingTask.Job == Job) {
                var tasks = startingTask.GetTaskData().nestedTasks;
                switch (startingTask.InstanceTaskType) {
                    case TaskType.Parallel:
                        foreach (var t in tasks)
                            toReturn.AddRange(GetFirstUnfinishedTasks(t));
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

        /// <summary>
        /// Return the current status of the held job.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string GetFloatieText(int index) {
            switch (Job.State) {
                case JobState.Available:
                    if (_jobNotAllowed)
                        return _jobNotAllowedText;
                    else if (_jobConsists.Count > 1) {
                        index %= _jobConsists.Count;
                        var sb = new StringBuilder(string.Format("The cars for job {0} are currently in {1} different consists on tracks", Job.ID, _jobConsists.Count));
                        for (var i = 0; i < _currentTracks.Count; i++) {
                            if (i > 0 && _currentTracks.Count > 2)
                                sb.Append(',');
                            if (i == _currentTracks.Count - 1)
                                sb.Append(" and");
                            sb.Append(' ');
                            var t = _currentTracks[i];
                            if (i == index) {
                                sb.AppendFormat("<color=#F29839>{0}</color>", t == null ? "(Unknown)" : t.TrackPartOnly);
                            } else {
                                sb.Append(t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                        }
                        sb.Append('.');
                        return sb.ToString();
                    } else if (_jobConsists.Count == 1) {
                        var t = _currentTracks[0];
                        var locoHooked = PlayerManager.LastLoco?.trainset == _jobConsists[0];
                        return string.Format("The cars for job {0} are in the same consist on track <color=#F29839>{1}</color>{2}.", Job.ID, t == null ? "(Unknown)" : t.TrackPartOnly, locoHooked ? " and have a locomotive attached" : "");
                    } else
                        return "The job cars could not be found... wtf?";
                case JobState.InProgress:
                    // Pretty much the same thing as JobState.Available except no job eligibility checking.
                    if (_jobConsists.Count > 1) {
                        index %= _jobConsists.Count;
                        var sb = new StringBuilder(string.Format("The cars for job {0} are currently in {1} different consists on tracks", Job.ID, _jobConsists.Count));
                        for (var i = 0; i < _currentTracks.Count; i++) {
                            if (i > 0 && _currentTracks.Count > 2)
                                sb.Append(',');
                            if (i == _currentTracks.Count - 1)
                                sb.Append(" and");
                            sb.Append(' ');
                            var t = _currentTracks[i];
                            if (i == index) {
                                sb.AppendFormat("<color=#F29839>{0}</color>", t == null ? "(Unknown)" : t.TrackPartOnly);
                            } else {
                                sb.Append(t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                        }
                        sb.Append('.');
                        return sb.ToString();
                    } else if (_jobConsists.Count == 1) {
                        var t = _currentTracks[0];
                        var locoHooked = PlayerManager.LastLoco?.trainset == _jobConsists[0];
                        return string.Format("The cars for job {0} are in the same consist on track <color=#F29839>{1}</color>{2}.", Job.ID, t == null ? "(Unknown)" : t.TrackPartOnly, locoHooked ? " and have a locomotive attached" : "");
                    } else if (_currentTasks > 0) // No cars in sight but tasks are unfinished.
                        return "The job cars could not be found... wtf?";
                    else // No cars in sight because no tasks found.
                        return "The job is probably complete. Try turning it in.";
                case JobState.Completed:
                    return "This job is completed.";
                case JobState.Abandoned:
                    return "This job is abandoned.";
                case JobState.Failed:
                    return "This job is failed.";
                case JobState.Expired:
                    return "This job is expired.";
                default:
                    return "This job state is unknown.";
            }
        }

        /// <summary>
        /// Return a transform to point to the the nth consist.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Transform GetPointerAt(int index) {
            if (_jobConsists.Count < 1 || _jobNotAllowed)
                return null;
            index %= _jobConsists.Count;
            var t = _jobConsists[index];
            if (t == PlayerManager.LastLoco?.trainset)
                return PlayerManager.LastLoco?.transform;
            // Get car in middle of set.
            // TODO: Raise pointer to middle of car height.
            return t.cars[t.cars.Count / 2].transform;
        }
    }
}
