using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PoshTasks.Cmdlets
{
    public abstract class TaskCmdlet<TIn, TOut> : Cmdlet where TIn : class
        where TOut : class
    {
        #region Parameters

        [Parameter(ValueFromPipeline = true)]
        public TIn[] InputObject { get; set; }

        #endregion

        #region Abstract methods

        /// <summary>
        /// Performs an action on <paramref name="server"/>
        /// </summary>
        /// <param name="input">The <see cref="object"/> to be processed; null if not processing input</param>
        /// <returns>A <see cref="T"/></returns>
        protected abstract TOut ProcessTask(TIn input = null);

        #endregion

        #region Virtual methods

        /// <summary>
        /// Generates a collection of tasks to be processed
        /// </summary>
        /// <returns>A collection of tasks</returns>
        protected virtual IEnumerable<Task<TOut>> GenerateTasks()
        {
            List<Task<TOut>> tasks = new List<Task<TOut>>();

            if (InputObject != null)
                foreach (TIn input in InputObject)
                    tasks.Add(Task.Run(() => ProcessTask(input)));
            else
                tasks.Add(Task.Run(() => ProcessTask()));

            return tasks;
        }

        /// <summary>
        /// Performs the pipeline output for this cmdlet
        /// </summary>
        /// <param name="result"></param>
        protected virtual void PostProcessTask(TOut result)
        {
            WriteObject(result, true);
        }

        #endregion

        #region Processing

        /// <summary>
        /// Processes cmdlet operation
        /// </summary>
        protected override void ProcessRecord()
        {
            IEnumerable<Task<TOut>> tasks = GenerateTasks();
            
            foreach (Task<Task<TOut>> bucket in Interleaved(tasks))
            {
                try
                {
                    Task<TOut> task = bucket.Result;
                    TOut result = task.Result;

                    PostProcessTask(result);
                }
                catch (Exception e) when (e is PipelineStoppedException || e is PipelineClosedException)
                {
                    // do nothing if pipeline stops
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().Name, ErrorCategory.NotSpecified, this));
                }
            }
        }

        /// <summary>
        /// Interleaves the tasks
        /// </summary>
        /// <param name="tasks">The collection of <see cref="Task{TOut}"/></param>
        /// <returns>An array of task tasks</returns>
        protected Task<Task<TOut>>[] Interleaved(IEnumerable<Task<TOut>> tasks)
        {
            TaskCompletionSource<Task<TOut>>[] buckets = new TaskCompletionSource<Task<TOut>>[tasks.Count()];
            Task<Task<TOut>>[] results = new Task<Task<TOut>>[buckets.Length];

            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TaskCompletionSource<Task<TOut>>();
                results[i] = buckets[i].Task;
            }

            int nextTaskIndex = -1;

            foreach (Task<TOut> task in tasks)
                task.ContinueWith(completed =>
                {
                    TaskCompletionSource<Task<TOut>> bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                    bucket.TrySetResult(completed);
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

            return results;
        }

        #endregion
    }
}
