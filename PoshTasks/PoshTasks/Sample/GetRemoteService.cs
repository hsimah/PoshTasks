using PoshTasks.Cmdlets;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.ServiceProcess;

namespace PoshTasks.Sample
{
    [Cmdlet(VerbsCommon.Get, "RemoteService")]
    public class GetRemoteService : TaskCmdlet<string, ServiceController[]>
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the collection of requested service names
        /// </summary>
        [Parameter]
        public string[] Name { get; set; }

        #endregion

        #region Processing

        /// <summary>
        /// Processes a single remote service lookup
        /// </summary>
        /// <param name="server">The remote machine name</param>
        /// <returns>A collection of <see cref="ServiceController"/>s from the remote machine</returns>
        protected override ServiceController[] ProcessTask(string server)
        {
            var services = string.IsNullOrEmpty(server) ? ServiceController.GetServices() : ServiceController.GetServices(server);

            return services.Where(s => Name == null || Name.Contains(s.DisplayName)).ToArray();
        }

        /// <summary>
        /// Generates custom service object and outputs to pipeline
        /// </summary>
        /// <param name="result">The collection of remote services</param>
        protected override void PostProcessTask(ServiceController[] result)
        {
            var services = new List<dynamic>();

            foreach (var service in result)
                services.Add(new
                {
                    Name = service.DisplayName,
                    Status = service.Status,
                    ComputerName = service.MachineName,
                    CanPause = service.CanPauseAndContinue
                });

            WriteObject(services, true);
        }

        #endregion
    }
}
