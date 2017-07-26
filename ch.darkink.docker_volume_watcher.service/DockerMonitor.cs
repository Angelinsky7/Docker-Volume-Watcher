using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace ch.darkink.docker_volume_watcher {
    public class DockerMonitor {

        public const String CONTAINER_STATE_RUNNING = "running";
        public const String MOUNT_POINT_MODE_BIND = "bind";

        private static readonly Regex m_SourceToHostPath = new Regex("^/([a-zA-Z])/(.*)$", RegexOptions.Compiled);

        private Dictionary<String, IList<DockerNotifier>> m_ContainerNotifier;
        private Timer m_Timer;
        private static readonly Object m_Lock = new Object();

        public DockerMonitor() {
            m_ContainerNotifier = new Dictionary<String, IList<DockerNotifier>>();
        }

        public void Start() {
            Console.WriteLine("Starting");

            m_Timer = new Timer() {
                Interval = 500,
                AutoReset = true,
                Enabled = true
            };
            m_Timer.Elapsed += M_Timer_Elapsed;
            m_Timer.Start();
        }

        public void Stop() {
            if (m_Timer != null) {
                m_Timer.Stop();
                m_Timer.Elapsed -= M_Timer_Elapsed;
                m_Timer.Dispose();
                m_Timer = null;
            }
            CleanNotifiers();

            Console.WriteLine("Stopped");
        }

        private void M_Timer_Elapsed(object sender, ElapsedEventArgs e) {
            lock (m_Lock) {
                IList<ContainerListResponse> newContainers = FindContainer();
                foreach (ContainerListResponse newContainer in newContainers.Where(p => p.State == CONTAINER_STATE_RUNNING)) {
                    if (!m_ContainerNotifier.ContainsKey(newContainer.ID)) {
                        m_ContainerNotifier.Add(newContainer.ID, WatchContainer(newContainer));
                        Console.WriteLine($"Add container {newContainer.ID} : {m_ContainerNotifier[newContainer.ID].Count()} notifier(s)");
                    }
                }
                foreach (var item in m_ContainerNotifier.ToList()) {
                    if (newContainers.Count(p => p.ID == item.Key) == 0) {
                        foreach (DockerNotifier notifier in item.Value.ToList()) {
                            notifier.Release();
                        }
                        item.Value.Clear();
                        m_ContainerNotifier.Remove(item.Key);
                        Console.WriteLine($"Remove container {item.Key}");
                    }
                }
            }
        }

        private IList<ContainerListResponse> FindContainer() {
            using (DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient()) {
                return client.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true }
                ).Result;
            }
        }

        private IList<DockerNotifier> WatchContainer(ContainerListResponse container) {
            List<DockerNotifier> r = new List<DockerNotifier>();
            foreach (MountPoint mount in container.Mounts) {
                String hostDirectory = GetHostDirectory(mount.Source);
                if (!String.IsNullOrEmpty(hostDirectory)) {
                    r.Add(new DockerNotifier(container.ID, hostDirectory, mount.Destination, (e) => { r.Remove(e); }));
                }
            }

            return r;
        }

        private String GetHostDirectory(String source) {
            MatchCollection matches = m_SourceToHostPath.Matches(source);
            if (matches.Count != 1 || matches[0].Groups.Count != 3) { return null; }
            return $"{matches[0].Groups[1]}:\\{matches[0].Groups[2]}";
        }

        private void CleanNotifiers() {
            if (m_ContainerNotifier != null) {
                foreach (var item in m_ContainerNotifier) {
                    foreach (DockerNotifier notifier in item.Value) {
                        notifier.Release();
                    }
                    item.Value.Clear();
                }
            }
        }
    }
}
