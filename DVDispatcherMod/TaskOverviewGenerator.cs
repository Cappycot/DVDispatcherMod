using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DV.Logic.Job;
using UnityEngine;

namespace DVDispatcherMod {
    public class TaskOverviewGenerator {
        private static readonly FieldInfo StationControllerStationRangeField = typeof(StationController).GetField("stationRange", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Dictionary<string, Color> _yardID2Color;

        public TaskOverviewGenerator() {
            _yardID2Color = StationController.allStations.ToDictionary(s => s.stationInfo.YardID, s => s.stationInfo.StationColor);
        }

        public string GetTaskOverview(Job job) {
            var nearestYardID = StationController.allStations.OrderBy(s => ((StationJobGenerationRange)StationControllerStationRangeField.GetValue(s)).PlayerSqrDistanceFromStationCenter).FirstOrDefault()?.stationInfo.YardID;

            return GenerateTaskOverview(0, job.tasks.First(), nearestYardID);
        }

        private string GenerateTaskOverview(int indent, Task task, string nearestYardID) {
            if (task.InstanceTaskType == TaskType.Parallel || task.InstanceTaskType == TaskType.Sequential) {
                var taskData = task.GetTaskData();

                //if (taskData.nestedTasks.Count == 1) {
                //    GenerateTaskOverview(indent, taskData.nestedTasks[0], sb);
                //} else {
                //    AppendTaskLine(indent, task, sb);

                return string.Join(Environment.NewLine, taskData.nestedTasks.Select(t => GenerateTaskOverview(indent + 1, t, nearestYardID)));
                //}
            } else {
                return GetTaskString(indent, task, nearestYardID);
            }
        }

        private string GetTaskString(int indent, Task task, string nearestYardID) {
            var taskData = task.GetTaskData();

            var stringBuilder = new StringBuilder();

            if (task.InstanceTaskType == TaskType.Parallel) {
                AppendIndented(indent, "Parallel", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Sequential) {
                AppendIndented(indent, "Sequential", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Transport) {
                AppendIndented(indent, $"Transport {FormatNumberOfCars(taskData.cars.Count)} cars from {FormatTrack(taskData.startTrack, nearestYardID)} to {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Warehouse) {
                if (taskData.warehouseTaskType == WarehouseTaskType.Loading) {
                    AppendIndented(indent, $"Load {FormatNumberOfCars(taskData.cars.Count)} at {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
                } else if (taskData.warehouseTaskType == WarehouseTaskType.Unloading) {
                    AppendIndented(indent, $"Unload {FormatNumberOfCars(taskData.cars.Count)} at {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
                } else {
                    AppendIndented(indent, "(unknown WarehouseTaskType)", stringBuilder);
                }
            } else {
                AppendIndented(indent, "(unknown TaskType)", stringBuilder);
            }

            if (task.state == TaskState.Done) {
                return GetColoredString(Color.green, stringBuilder.ToString());
            } else if (task.state == TaskState.Failed) {
                return GetColoredString(Color.red, stringBuilder.ToString());
            } else {
                return stringBuilder.ToString();
            }
        }

        private static string GetColoredString(Color color, string content) {
            var colorString = GetHexColorComponent(color.r) + GetHexColorComponent(color.g) + GetHexColorComponent(color.b);
            return $"<color=#{colorString}>{content}</color>";
        }

        private static string GetHexColorComponent(float colorComponent) {
            return Convert.ToString((int)(255 * colorComponent), 16);
        }

        private string FormatTrack(Track track, string nearestYardID) {
            if (track.ID.yardId == nearestYardID) {
                return track.ID.TrackPartOnly;
            } else {
                return GetColoredString(_yardID2Color[track.ID.yardId], track.ID.FullDisplayID);
            }
        }

        private static string FormatNumberOfCars(int count) {
            if (count == 1) {
                return "1 car";
            } else {
                return count + " cars";
            }
        }

        private static void AppendIndented(int indent, string value, StringBuilder sb) {
            for (var i = 0; i < indent; i += 1) {
                sb.Append("  ");
            }
            sb.Append("- ");
            sb.Append(value);
        }
    }
}