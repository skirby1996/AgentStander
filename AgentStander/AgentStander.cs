using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace AgentStander
    {
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class AgentStander : StatelessService
        {
        public AgentStander (StatelessServiceContext context)
            : base(context)
            {
            }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners ()
            {
            return new ServiceInstanceListener[0];
            }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync (CancellationToken cancellationToken)
            {
            Random rand = new Random();
            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            CodePackage codePackage = activationContext.GetCodePackageObject("Code");

            string nodePath = Path.Join(codePackage.Path, "node.exe");
            string scriptPath = Path.Join(codePackage.Path, "dummy.js");

            IDictionary<Guid, Process> processes = new Dictionary<Guid, Process>();

            IList<Guid> toDelete;
            while ( true )
                {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, $"Running processes: {processes.Count}");

                // Randomly add new procesess
                for ( int i = 0; i < rand.Next(5); i++ )
                    {
                    Process process = new Process();
                    process.StartInfo.FileName = nodePath;
                    process.StartInfo.Arguments = $"{scriptPath} {rand.Next(3000, 10000)}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    processes.Add(Guid.NewGuid(), process);
                    process.Start();
                    }

                // Remove finished processes
                toDelete = new List<Guid>();
                foreach ( Guid key in processes.Keys )
                    {
                    Process process = processes[key];
                    if ( process.HasExited )
                        {
                        TimeSpan elapsed = process.ExitTime.Subtract(process.StartTime);
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"Process {key.ToString()} exited in {elapsed.ToString()}");
                        toDelete.Add(key);
                        }
                    }

                foreach ( Guid key in toDelete )
                    processes.Remove(key);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
    }
