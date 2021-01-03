using DV.Logic.Job;
using DV.ServicePenalty;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DVDispatcherMod
{
    public class JobDispatch
    {
        public Job job { get; private set; }

        private bool jobNotAllowed;
        private string jobNotAllowedText;

        private List<Car> jobCars;
        private Dictionary<Car, bool> jobCarsUsed;
        private List<Trainset> jobConsists; // Important consists to point at.
        // private Trainset jobConsistUsed; // Trainset containing player's last loco.
        private int currentTasks;
        private List<TrackID> currentTracks;
        // private Dictionary<TrackID, bool> destinationTracks; // Check if dropoffs are occupied.
        // private List<Trainset> destinationConsists; // Consists blocking the destination tracks.

        public JobDispatch(Job job)
        {
            this.job = job;
            jobCars = new List<Car>();
            jobCarsUsed = new Dictionary<Car, bool>();
            jobConsists = new List<Trainset>();
            currentTasks = 0;
            currentTracks = new List<TrackID>();
            UpdateJobCars();
            UpdateJobPrivilege();
        }

        public void UpdateJobCars()
        {
            jobCars.Clear();
            jobCarsUsed.Clear();
            jobConsists.Clear();
            // jobConsistUsed = null;
            currentTracks.Clear();
            List<Task> tasks = GetFirstUnfinishedTasks();
            currentTasks = tasks.Count;
            foreach (Task t in tasks)
                jobCars.AddRange(t.GetTaskData().cars);
            foreach (Car c in jobCars)
                jobCarsUsed[c] = false;
            foreach (Car car in jobCars)
            {
                if (!jobCarsUsed[car])
                {
                    TrackID trackID = car.CurrentTrack?.ID;
                    if( !SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(car, out TrainCar trainCar) ) continue;

                    Trainset trainset = trainCar.trainset;
                    foreach (TrainCar tc in trainset.cars)
                    {
                        Car c = tc.logicCar;
                        if (c != null)
                        {
                            if (jobCarsUsed.ContainsKey(c))
                                jobCarsUsed[c] = true;
                            if (trackID == null)
                                trackID = c.CurrentTrack?.ID;
                        }
                    }
                    jobConsists.Add(trainset);
                    currentTracks.Add(trackID);
                }
            }
        }

        public void UpdateJobTracks()
        {

        }

        // TODO: Find different place to define and call?
        public void UpdateJobPrivilege()
        {
            jobNotAllowed = false;
            if (PlayerJobs.Instance.currentJobs.Count >= LicenseManager.GetNumberOfAllowedConcurrentJobs())
            {
                jobNotAllowed = true;
                jobNotAllowedText = "You already have the maximum number of active jobs.";
            }
            else if (!LicenseManager.IsLicensedForJob(job.requiredLicenses))
            {
                jobNotAllowed = true;
                jobNotAllowedText = "You don't have the required license(s) for this job.";
            }
            else if (!SingletonBehaviour<CareerManagerDebtController>.Instance.IsPlayerAllowedToTakeJob())
            {
                jobNotAllowed = true;
                jobNotAllowedText = "You still have fees to pay off in the Career Manager.";
            }
        }

        public List<Task> GetFirstUnfinishedTasks()
        {
            return GetFirstUnfinishedTasks(job.tasks.First());
        }

        /// <summary>
        /// Figure out the current tasks.
        /// </summary>
        /// <param name="startingTask"></param>
        /// <returns></returns>
        private List<Task> GetFirstUnfinishedTasks(Task startingTask)
        {
            List<Task> toReturn = new List<Task>();
            if (startingTask != null && startingTask.state == TaskState.InProgress && startingTask.Job == job)
            {
                List<Task> tasks = startingTask.GetTaskData().nestedTasks;
                switch (startingTask.InstanceTaskType)
                {
                    case TaskType.Parallel:
                        foreach (Task t in tasks)
                            toReturn.AddRange(GetFirstUnfinishedTasks(t));
                        break;
                    case TaskType.Sequential:
                        foreach (Task t in tasks)
                        {
                            if (t.state == TaskState.InProgress)
                            {
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
        public string GetFloatieText(int index)
        {
            switch (job.State)
            {
                case JobState.Available:
                    if (jobNotAllowed)
                        return jobNotAllowedText;
                    else if (jobConsists.Count > 1)
                    {
                        index %= jobConsists.Count;
                        StringBuilder sb = new StringBuilder(string.Format("The cars for job {0} are currently in {1} different consists on tracks", job.ID, jobConsists.Count));
                        for (int i = 0; i < currentTracks.Count; i++)
                        {
                            if (i > 0 && currentTracks.Count > 2)
                                sb.Append(',');
                            if (i == currentTracks.Count - 1)
                                sb.Append(" and");
                            sb.Append(' ');
                            TrackID t = currentTracks[i];
                            if (i == index)
                            {
                                sb.AppendFormat("<color=#F29839>{0}</color>", t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                            else
                            {
                                sb.Append(t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                        }
                        sb.Append('.');
                        return sb.ToString();
                    }
                    else if (jobConsists.Count == 1)
                    {
                        TrackID t = currentTracks[0];
                        bool locoHooked = PlayerManager.LastLoco?.trainset == jobConsists[0];
                        return string.Format("The cars for job {0} are in the same consist on track <color=#F29839>{1}</color>{2}.", job.ID, t == null ? "(Unknown)" : t.TrackPartOnly, locoHooked ? " and have a locomotive attached" : "");
                    }
                    else
                        return "The job cars could not be found... wtf?";
                case JobState.InProgress:
                    // Pretty much the same thing as JobState.Available except no job eligibility checking.
                    if (jobConsists.Count > 1)
                    {
                        index %= jobConsists.Count;
                        StringBuilder sb = new StringBuilder(string.Format("The cars for job {0} are currently in {1} different consists on tracks", job.ID, jobConsists.Count));
                        for (int i = 0; i < currentTracks.Count; i++)
                        {
                            if (i > 0 && currentTracks.Count > 2)
                                sb.Append(',');
                            if (i == currentTracks.Count - 1)
                                sb.Append(" and");
                            sb.Append(' ');
                            TrackID t = currentTracks[i];
                            if (i == index)
                            {
                                sb.AppendFormat("<color=#F29839>{0}</color>", t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                            else
                            {
                                sb.Append(t == null ? "(Unknown)" : t.TrackPartOnly);
                            }
                        }
                        sb.Append('.');
                        return sb.ToString();
                    }
                    else if (jobConsists.Count == 1)
                    {
                        TrackID t = currentTracks[0];
                        bool locoHooked = PlayerManager.LastLoco?.trainset == jobConsists[0];
                        return string.Format("The cars for job {0} are in the same consist on track <color=#F29839>{1}</color>{2}.", job.ID, t == null ? "(Unknown)" : t.TrackPartOnly, locoHooked ? " and have a locomotive attached" : "");
                    }
                    else if (currentTasks > 0) // No cars in sight but tasks are unfinished.
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
        public Transform GetPointerAt(int index)
        {
            if (jobConsists.Count < 1 || jobNotAllowed)
                return null;
            index %= jobConsists.Count;
            Trainset t = jobConsists[index];
            if (t == PlayerManager.LastLoco?.trainset)
                return PlayerManager.LastLoco?.transform;
            // Get car in middle of set.
            // TODO: Raise pointer to middle of car height.
            return t.cars[t.cars.Count / 2].transform;
        }
    }
}
