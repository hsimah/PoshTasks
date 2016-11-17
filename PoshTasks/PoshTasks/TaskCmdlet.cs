using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace PoshTasks.Cmdlets
{
    public abstract class TaskCmdlet<TIn, TOut> : Cmdlet
        where TIn : class
        where TOut : class
    {
        [Parameter(ValueFromPipeline = true, ParameterSetName = "InputObject")]
        public virtual TIn[] InputObject { get; set; }

        /// <summary>
        /// Performs an action on <paramref name="server"/>
        /// </summary>
        /// <param name="input">The <see cref="object"/> to be processed; null if not processing input</param>
        /// <returns>A <see cref="T"/></returns>
        protected abstract TOut ProcessTask(TIn input = null);

        /// <summary>
        /// Creates a collection of tasks to be processed
        /// </summary>
        /// <returns>A collection of tasks</returns>
        [Obsolete]
        protected virtual IEnumerable<Task<TOut>> GenerateTasks()
        {
            return CreateProcessTasks();
        }

        /// <summary>
        /// Creates a collection of tasks to be processed
        /// </summary>
        /// <returns>A collection of tasks</returns>
        protected virtual IEnumerable<Task<TOut>> CreateProcessTasks()
        {
            if (InputObject == null)
            {
                yield return Task.Run(() => ProcessTask());
                yield break;
            }

            foreach (var input in InputObject)
            {
                yield return Task.Run(() => ProcessTask(input));
            }
        }

        /// <summary>
        /// Performs the pipeline output for this cmdlet
        /// </summary>
        /// <param name="result"></param>
        protected virtual void PostProcessTask(TOut result)
        {
            WriteObject(result, true);
        }

        /// <summary>
        /// Processes cmdlet operation
        /// </summary>
        protected override void ProcessRecord()
        {
            var tasks = CreateProcessTasks();

            var results = Task.WhenAll(tasks.ToArray());

            results.Wait();

            foreach (var result in results.Result)
            {
                try
                {
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
    }
}
