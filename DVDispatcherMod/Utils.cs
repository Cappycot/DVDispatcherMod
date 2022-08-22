using DV.Logic.Job;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DVDispatcherMod
{
    class Utils
    {
        public static string ColorText(Color color, string text)
        {
            string r = ((int)(color.r * 256.0f)).ToString("X2");
            string g = ((int)(color.g * 256.0f)).ToString("X2");
            string b = ((int)(color.b * 256.0f)).ToString("X2");
            return string.Format("<color=#{0}{1}{2}>{3}</color>", r, g, b, text);
        }

        /// <summary>
        /// Use a hex string preceded by a '#' for the color field.
        /// e.g. #C0FFEE
        /// </summary>
        /// <param name="color"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ColorText(string color, string text)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        public static TrainCar GetClosestCar(List<Car> group)
        {
            if (group.Count < 1)
                return null;
            TrainCar closestCar = null;
            float closestDist = float.MaxValue;
            foreach (Car car in group)
            {
                if (!SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(group.First(), out TrainCar trainCar))
                    continue;
                float dist = (trainCar.transform.position - PlayerManager.PlayerTransform.position).sqrMagnitude;
                if (closestDist > dist)
                {
                    closestCar = trainCar;
                    closestDist = dist;
                }
            }
            return closestCar;
        }

        public static TrainCar GetClosestFirstLastCar(List<Car> group)
        {
            if (group.Count < 1)
                return null;
            if (!SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(group.First(), out TrainCar first))
                return null;
            if (!SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(group.Last(), out TrainCar last))
                return first;
            float dist = (first.transform.position - PlayerManager.PlayerTransform.position).sqrMagnitude;
            if (dist > (last.transform.position - PlayerManager.PlayerTransform.position).sqrMagnitude)
                return last;
            return first;
        }

        /// <summary>
        /// Look up logic cars by carId.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="carIds"></param>
        /// <returns></returns>
        public static List<Car> LookupCars(Job job, HashSet<string> carIds)
        {
            Dictionary<string, Car> s2c = new Dictionary<string, Car>();
            foreach (Task task in job.tasks)
            {
                LookupCars(task, carIds, s2c);
                if (carIds.Count == s2c.Count)
                    break;
            }
            return new List<Car>(s2c.Values);
        }

        private static void LookupCars(Task task, HashSet<string> carIds, Dictionary<string, Car> s2c)
        {
            TaskData taskData = task.GetTaskData();
            switch (task.InstanceTaskType)
            {
                case TaskType.Parallel:
                case TaskType.Sequential:
                    foreach (Task next in task.GetTaskData().nestedTasks)
                    {
                        LookupCars(next, carIds, s2c);
                        if (carIds.Count == s2c.Count)
                            break;
                    }
                    break;
                default:
                    foreach (Car car in taskData.cars)
                    {
                        if (carIds.Contains(car.ID))
                            s2c.Add(car.ID, car);
                        if (carIds.Count == s2c.Count)
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Uses TrackID.TrackPartOnly for the trackId.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="yardId"></param>
        /// <param name="trackId"></param>
        /// <returns></returns>
        public static Track LookupTrack(Job job, string yardId, string trackId)
        {
            foreach (Task task in job.tasks)
            {
                Track track = LookupTrack(task, yardId, trackId);
                if (track != null)
                    return track;
            }
            return null;
        }

        private static Track LookupTrack(Task task, string yardId, string trackId)
        {
            TaskData taskData = task.GetTaskData();
            switch (task.InstanceTaskType)
            {
                case TaskType.Parallel:
                case TaskType.Sequential:
                    foreach (Task next in task.GetTaskData().nestedTasks)
                    {
                        Track track = LookupTrack(next, yardId, trackId);
                        if (track != null)
                            return track;
                    }
                    break;
                default:
                    Track start = taskData.startTrack;
                    Track dest = taskData.destinationTrack;
                    if (start != null && start.ID.yardId == yardId && start.ID.TrackPartOnly == trackId)
                        return start;
                    else if (dest != null && dest.ID.yardId == yardId && dest.ID.TrackPartOnly == trackId)
                        return dest;
                    break;
            }
            return null;
        }

        public static string PlaceSuffix(int place)
        {
            switch (place % 100)
            {
                case 11:
                case 12:
                case 13:
                    return "th";
                default:
                    switch (place % 10)
                    {
                        case 1:
                            return "st";
                        case 2:
                            return "nd";
                        case 3:
                            return "rd";
                        default:
                            return "th";
                    }
            }
        }
    }
}
