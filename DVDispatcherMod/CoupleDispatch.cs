using DV.Logic.Job;
using DV.RenderTextureSystem.BookletRender;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DVDispatcherMod
{

    public class CoupleDispatch : IDispatch
    {
        private struct Group
        {
            public List<Car> cars;
            public int consistPos;
            public int numCars;

            public Group(List<Car> cars, int numCars)
            {
                this.cars = cars;
                this.consistPos = -1;
                this.numCars = numCars;
            }
        }
        
        // Set of cars. The bool is used for cataloging purposes.
        private Dictionary<Car, bool> cars;
        // Lists of contiguous cars.
        private List<Group> groups;
        private Track track;
        private bool uncouple;

        public CoupleDispatch(Job job, TaskTemplatePaperData ttpd, bool uncouple)
        {
            HashSet<string> carIds = new HashSet<string>();
            ttpd.cars.ForEach(c => carIds.Add(c.Item2));
            this.cars = new Dictionary<Car, bool>();
            Utils.LookupCars(job, carIds).ForEach(c => cars.Add(c, false));
            this.groups = new List<Group>();
            this.track = Utils.LookupTrack(job, ttpd.yardId, ttpd.trackId);
            this.uncouple = uncouple;
        }

        string IDispatch.GetFloatieText(int index)
        {
            if (groups.Count < 1)
            {
                if (uncouple)
                    return "This task has been completed.";
                else
                    return "These cars could not be found... wtf?";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                // Only occurs when cars are omitted due to being at uncouple destination.
                int destCars = cars.Count - groups.Aggregate(0, (c, g) => c + g.cars.Count);
                if (destCars > 0)
                {
                    sb.AppendFormat("At Destination: {0} Car", destCars);
                    if (destCars != 1)
                        sb.Append('s');
                }
                // Go through locations of all groups of cars.
                int i = 0;
                // For groups of cars part of the last consist.
                if (groups.Any(g => g.consistPos != -1))
                {
                    if (destCars > 0)
                        sb.Append('\n');
                    sb.Append("In Current Consist:");
                    for (; groups[i].consistPos != -1 && i < groups.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');
                        sb.Append(' ');
                        int place = groups[i].consistPos;
                        if (i == index)
                            sb.Append(Utils.ColorText("#F29839", string.Format("{0}{1}", place, Utils.PlaceSuffix(place))));
                        else
                        {
                            sb.Append(place);
                            sb.Append(Utils.PlaceSuffix(place));
                        }
                    }
                }
                // For detached groups of cars.
                int j = i;
                if (groups.Any(g => g.consistPos == -1))
                {
                    if (i > 0)
                        sb.Append("\nOther Locations:");
                    else
                        sb.Append("Car Locations:");
                    for (; i < groups.Count; i++)
                    {
                        Car car = Utils.GetClosestFirstLastCar(groups[i].cars)?.logicCar;
                        // TODO: Null checks.
                        if (i > j)
                            sb.Append(',');
                        sb.Append(' ');
                        Track t = car.CurrentTrack;
                        if (t == track)
                        {
                            t = car.FrontBogieTrack;
                            if (t == track)
                                t = car.RearBogieTrack;
                        }
                        if (i == index)
                            sb.Append(Utils.ColorText("#F29839", t.ID.TrackPartOnly));
                        else
                        {
                            sb.Append(t.ID.TrackPartOnly);
                        }
                    }
                }
                return sb.ToString();
            }
        }

        Transform IDispatch.GetPointerAt(int index)
        {
            if (groups.Count < 1)
            {
                if (uncouple)
                    return Utils.GetClosestCar(cars.Keys.ToList())?.transform;
                else
                    return null; // Should not happen.
            }
            else if (groups.Count == 1)
            {
                return Utils.GetClosestFirstLastCar(groups.First().cars)?.transform;
            }
            else
            {
                return Utils.GetClosestFirstLastCar(groups[index % groups.Count].cars)?.transform;
            }
        }

        void IDispatch.UpdateDispatch()
        {
            // Recatalogue groups of cars for this task.
            groups.Clear();
            foreach (Car car in cars.Keys)
                cars[car] = false;
            // Will go through up to cars.Count different groups.
            List<Group> detachedGroups = new List<Group>();
            foreach (Car car in cars.Keys)
            {
                if (cars[car] || !SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(car, out TrainCar trainCar))
                    continue;
                while (!cars[car])
                {
                    // Comb through the trainset for contiguous groups of cars that are associated with this task.
                    Trainset trainset = trainCar.trainset;

                    // List of cars not on the destination rail.
                    List<Car> currentCars = new List<Car>();
                    // Total number of cars in group including destination rail.
                    int numCars = 0;
                    bool inGroup = false; // Tracks a group of contiguous cars.
                    int i = 0;
                    //
                    for (; i < trainset.cars.Count; i++)
                    {
                        TrainCar tc = trainset.cars[i];
                        Car c = tc.logicCar;
                        bool isGroupCar = c != null && cars.ContainsKey(c) && !cars[c];
                        bool reachedDest = isGroupCar && uncouple && c.CurrentTrack == track && c.BogiesOnSameTrack;
                        if (reachedDest)
                            cars[c] = true;
                        // If this is an uncoupling task, only add cars that have not reached dest.
                        if (isGroupCar && !reachedDest)
                        {
                            cars[c] = true;
                            currentCars.Add(c);
                            inGroup = true;
                            numCars++;
                        }
                        else if (inGroup)
                            break;
                    }
                    // Add current group to list of car groups.
                    if (currentCars.Count > 0)
                    {
                        Group group = new Group(currentCars, numCars);
                        if (trainset == PlayerManager.LastLoco?.trainset)
                        {
                            int place = i - currentCars.Count + 1;
                            if (!trainset.cars.First().IsLoco && trainset.cars.Last().IsLoco)
                            {
                                currentCars.Reverse();
                                place = trainset.cars.Count - i + 1;
                            }
                            group.consistPos = place;
                            groups.Add(group);
                        }
                        else
                            detachedGroups.Add(group);
                    }
                }
            }
            groups.AddRange(detachedGroups);
        }
    }
}
