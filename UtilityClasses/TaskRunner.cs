using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExileCore2.Shared;

namespace ThisIsTheWaystone.UtilityClasses
{
    /// <summary>
    /// Task runner for managing async operations
    /// Based on MapCrafter's TaskRunner pattern
    /// </summary>
    public static class TaskRunner
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> Tasks = new();

        /// <summary>
        /// Run an async task with a name for tracking
        /// </summary>
        public static void Run(Func<SyncTask<bool>> task, string name)
        {
            var cts = new CancellationTokenSource();
            Tasks[name] = cts;
            
            Task.Run(async () =>
            {
                var sTask = task();
                while (sTask != null && !cts.Token.IsCancellationRequested)
                {
                    TaskUtils.RunOrRestart(ref sTask, () => null);
                    await TaskUtils.NextFrame();
                }

                Tasks.TryRemove(new KeyValuePair<string, CancellationTokenSource>(name, cts));
            });
        }

        /// <summary>
        /// Stop a running task by name
        /// </summary>
        public static void Stop(string name)
        {
            if (Tasks.TryGetValue(name, out var cts))
            {
                cts.Cancel();
                Tasks.TryRemove(new KeyValuePair<string, CancellationTokenSource>(name, cts));
            }
        }

        /// <summary>
        /// Check if a task is currently running
        /// </summary>
        public static bool Has(string name)
        {
            return Tasks.ContainsKey(name);
        }

        /// <summary>
        /// Stop all running tasks
        /// </summary>
        public static void StopAll()
        {
            foreach (var task in Tasks.Values)
            {
                task.Cancel();
            }
            Tasks.Clear();
        }
    }
}
