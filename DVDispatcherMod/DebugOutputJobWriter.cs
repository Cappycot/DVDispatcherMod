using System.Linq;
using DV.Logic.Job;

namespace DVDispatcherMod {
    public static class DebugOutputJobWriter {
        public static void DebugOutputJob(Job job) {
            DebugLogIndented(0, job.GetType().Name, job.ID);
            DebugLogIndented(1, "jobType", job.jobType);
            DebugLogIndented(1, "State", job.State);
            DebugLogIndented(1, "chainData");
            DebugLogIndented(2, "chainOriginYardId", job.chainData.chainOriginYardId);
            DebugLogIndented(2, "chainDestinationYardId", job.chainData.chainDestinationYardId);
            DebugLogIndented(1, "tasks");
            foreach (var jobTask in job.tasks) {
                DebugOutputTask(2, jobTask);
            }
        }

        private static void DebugOutputTask(int indent, Task jobTask) {
            DebugLogIndented(indent, jobTask.GetType().Name);
            DebugLogIndented(indent + 1, "InstanceTaskType", jobTask.InstanceTaskType);
            DebugLogIndented(indent + 1, "state", jobTask.state);

            var taskData = jobTask.GetTaskData();
            if (taskData.cars != null) {
                DebugLogIndented(indent + 1, "cars", string.Join(", ", taskData.cars.Select(c => c.ID)));
            }
            DebugLogIndented(indent + 1, "startTrack", taskData.startTrack?.ID?.FullDisplayID);
            DebugLogIndented(indent + 1, "destinationTrack", taskData.destinationTrack?.ID?.FullDisplayID);
            DebugLogIndented(indent + 1, "warehouseTaskType", taskData.warehouseTaskType);

            if (taskData.nestedTasks != null) {
                if (taskData.nestedTasks.Any()) {
                    DebugLogIndented(indent + 1, "nestedTasks (" + taskData.nestedTasks.Count + ")");

                    foreach (var nestedTask in taskData.nestedTasks) {
                        DebugOutputTask(indent + 2, nestedTask);
                    }
                }
            }
        }

        private static void DebugLogIndented(int indent, string name, object value = null) {
            var content = value != null ? (name + ": " + value) : name;
            Main.ModEntry.Logger.Log(string.Join("", Enumerable.Repeat("    ", indent)) + content);
        }
    }
}