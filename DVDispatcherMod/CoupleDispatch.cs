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

            public Group(List<Car> cars)
            {
                this.cars = cars;
                this.consistPos = -1;
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

        /*
         * Cars are in same group or k different groups.
         * Groups are part of last loco trainset or separated.
         * 
         * "These cars are in k different consists on tracks..."
         *  - Point alternate between consists.
         *  
         * "These cars are on track xxx."
         *  - Point to closest first/last car and get track id of closest car.
         *  
         * "These cars are kth in your current consist."
         *  - Point to closest first/last car.
         */
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
                    sb.AppendFormat("{0} car", destCars);
                    if (destCars > 1)
                        sb.Append("s are");
                    else
                        sb.Append(" is");
                    sb.Append(" already on the destination track.");
                }
                // Go through locations of all groups of cars.
                int i = 0;
                // For groups of cars part of the last consist.
                if (groups.Any(g => g.consistPos != -1))
                {
                    for (; groups[i].consistPos != -1 && i < groups.Count; i++)
                    {

                    }
                }
                else
                {
                    sb.Append(" ");
                }
                // For detached groups of cars.
                if (groups.Any(g => g.consistPos == -1))
                {
                    for (; i < groups.Count; i++)
                    {

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
                {
                    return null;
                }
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
            foreach (Car c in cars.Keys)
                cars[c] = false;
            foreach (Car c in cars.Keys)
            {
                bool test = uncouple && c.BogiesOnSameTrack && c.CurrentTrack == track;
            }
            // Will go through up to cars.Count different groups.
            List<Group> detachedGroups = new List<Group>();
            foreach (Car car in cars.Keys)
            {
                if (cars[car] || !SingletonBehaviour<IdGenerator>.Instance.logicCarToTrainCar.TryGetValue(car, out TrainCar trainCar))
                    continue;
                while (!cars[car])
                {
                    /*
                     * Comb through the trainset for contiguous groups of cars that are associated with this task.
                     */
                    Trainset trainset = trainCar.trainset;

                    List<Car> currentCars = new List<Car>();
                    bool inGroup = false;

                    for (int i = 0; i < trainset.cars.Count; i++)
                    {
                        TrainCar tc = trainset.cars[i];
                        Car c = tc.logicCar;
                        if (c != null && cars.ContainsKey(c) && !cars[c])
                        {
                            cars[c] = true;
                            // If this is an uncoupling dispatch, only add if car is not on destination.
                            if (!uncouple || c.CurrentTrack != track || !c.BogiesOnSameTrack)
                                currentCars.Add(c);
                            inGroup = true;
                        }
                        else if (inGroup)
                        {
                            if (currentCars.Count > 0)
                            {
                                Group group = new Group(currentCars);
                                if (trainset == PlayerManager.LastLoco?.trainset)
                                {
                                    int place = i - currentCars.Count;
                                    if (!trainset.cars.First().IsLoco && trainset.cars.Last().IsLoco)
                                    {
                                        currentCars.Reverse();
                                        place = trainset.cars.Count - i;
                                    }
                                    group.consistPos = place;
                                    groups.Add(group);
                                }
                                else
                                    detachedGroups.Add(group);
                            }
                            break;
                        }
                    }
                }
            }
            groups.AddRange(detachedGroups);
        }
    }
}
