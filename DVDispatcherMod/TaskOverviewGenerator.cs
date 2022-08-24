using System.Linq;
using System.Text;
using DV.Logic.Job;

namespace DVDispatcherMod {
    public static class TaskOverviewGenerator {
        public static string GetTaskOverview(Job job) {
            var stringBuilder = new StringBuilder();
            GenerateTaskOverview(0, job.tasks.First(), stringBuilder);
            return stringBuilder.ToString();
        }

        private static void GenerateTaskOverview(int indent, Task task, StringBuilder sb) {
            AppendTaskLine(indent, task, sb);

            if (task.InstanceTaskType == TaskType.Parallel || task.InstanceTaskType == TaskType.Sequential) {
                var taskData = task.GetTaskData();

                foreach (var nestedTask in taskData.nestedTasks) {
                    GenerateTaskOverview(indent + 1, nestedTask, sb);
                }
            }
        }

        private static void AppendTaskLine(int indent, Task task, StringBuilder sb) {
            var taskData = task.GetTaskData();
            if (task.state == TaskState.Done) {
                sb.Append("<color=#00FF00>");
            } else if (task.state == TaskState.Failed) {
                sb.Append("<color=#FF0000>");
            }

            if (task.InstanceTaskType == TaskType.Parallel) {
                AppendIndented(indent, "Parallel", sb);
            } else if (task.InstanceTaskType == TaskType.Sequential) {
                AppendIndented(indent, "Sequential", sb);
            } else if (task.InstanceTaskType == TaskType.Transport) {
                AppendIndented(indent, $"Transport {taskData.cars.Count} cars from {taskData.startTrack.ID.TrackPartOnly} to {taskData.destinationTrack.ID.TrackPartOnly}", sb);
            } else if (task.InstanceTaskType == TaskType.Warehouse) {
                if (taskData.warehouseTaskType == WarehouseTaskType.Loading) {
                    AppendIndented(indent, $"Load {taskData.cars.Count} at {taskData.destinationTrack.ID.TrackPartOnly}", sb);
                } else if (taskData.warehouseTaskType == WarehouseTaskType.Unloading) {
                    AppendIndented(indent, $"Unload {taskData.cars.Count} at {taskData.destinationTrack.ID.TrackPartOnly}", sb);
                } else {
                    AppendIndented(indent, "(unknown WarehouseTaskType)", sb);
                }
            } else {
                AppendIndented(indent, "(unknown TaskType)", sb);
            }

            if (task.state == TaskState.Done || task.state == TaskState.Failed) {
                sb.Append("</color>");
            }

            sb.AppendLine();
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