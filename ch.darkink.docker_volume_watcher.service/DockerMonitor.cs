using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;

namespace ch.darkink.docker_volume_watcher {
    public class DockerMonitor {

        public const String CONTAINER_STATE_RUNNING = "running";
        public const String MOUNT_POINT_MODE_BIND = "bind";

        private static readonly Regex m_SourceToHostPath = new Regex("^/([a-zA-Z])/(.*)$", RegexOptions.Compiled);
        private Dictionary<String, IList<DockerNotifier>> m_ContainerNotifier;
        private Timer m_Timer;
        //private static readonly Object m_Lock = new Object();
        private EventLog m_Log;
        private Int32 m_PollingInterval;
        private Int32 m_OldPollingInterval;
        private Int32 m_DockerPollingErrorCount;
        private Boolean m_IsIgnorefileMandatory;
        private Int32 m_NotifierAction;

        public DockerMonitor(EventLog eventLog, Int32 pollingInterval, Boolean isIgnorefileMandatory, Int32 notifierAction) {
            m_OldPollingInterval = -1;
            m_Log = eventLog;

            m_PollingInterval = pollingInterval;
            m_IsIgnorefileMandatory = isIgnorefileMandatory;
            m_NotifierAction = notifierAction;

            m_ContainerNotifier = new Dictionary<String, IList<DockerNotifier>>();
        }

        public void Start() {
            m_Timer = new Timer() {
                Interval = m_PollingInterval,
                AutoReset = true,
                Enabled = false
            };
            m_Timer.Elapsed += M_Timer_Elapsed;
            m_Timer.Start();
            LogMessage("Monitor started");
        }

        public void Stop() {
            if (m_Timer != null) {
                m_Timer.Stop();
                m_Timer.Elapsed -= M_Timer_Elapsed;
                m_Timer.Dispose();
                m_Timer = null;
            }
            CleanNotifiers();
            LogMessage("Monitor stopped");
        }

        private void M_Timer_Elapsed(Object sender, ElapsedEventArgs e) {
            //lock (m_Lock) {
            IList<ContainerListResponse> newContainers = FindContainer();
            if (newContainers != null) {
                foreach (var item in m_ContainerNotifier.ToList()) {
                    if (newContainers.Count(p => p.ID == item.Key) == 0) {
                        foreach (DockerNotifier notifier in item.Value.ToList()) {
                            notifier.Release();
                        }
                        item.Value.Clear();
                        m_ContainerNotifier.Remove(item.Key);
                        LogMessage($"Remove container {item.Key}");
                    }
                }
                foreach (ContainerListResponse newContainer in newContainers.Where(p => p.State == CONTAINER_STATE_RUNNING)) {
                    if (!m_ContainerNotifier.ContainsKey(newContainer.ID)) {
                        m_ContainerNotifier.Add(newContainer.ID, WatchContainer(newContainer));

                        LogMessage($"Add container {newContainer.ID} : {m_ContainerNotifier[newContainer.ID].Count()} notifier(s)");
                    }
                }
            }
            if (m_DockerPollingErrorCount > 100) {
                m_DockerPollingErrorCount = 0;
                if (m_OldPollingInterval == -1) {
                    m_OldPollingInterval = m_PollingInterval;
                }
                m_PollingInterval *= 10;
                LogMessage($"Too many errors from the docker daemon, changing the poll interval to {m_PollingInterval}ms");
                Stop();
                Start();
            } else if (newContainers != null && m_OldPollingInterval != -1) {
                m_DockerPollingErrorCount = 0;
                m_PollingInterval = m_OldPollingInterval;
                m_OldPollingInterval = -1;
                LogMessage($"Ok, docker daemon is back, reverting the poll interval to {m_PollingInterval}ms");
                Stop();
                Start();
            }
            //}
        }

        private IList<ContainerListResponse> FindContainer() {
            using (DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient()) {
                IList<ContainerListResponse> r = null;
                try {
                    r = client.Containers.ListContainersAsync(
                        new ContainersListParameters { All = true }
                    ).Result;
                    m_DockerPollingErrorCount = 0;
                } catch (Exception ex) {
                    m_DockerPollingErrorCount++;
                    LogMessage($"Cannot connect to docker deamon: {ex.InnerException.Message ?? ex.Message}", EventLogEntryType.Error);
                }
                return r;
            }
        }

        private IList<DockerNotifier> WatchContainer(ContainerListResponse container) {
            List<DockerNotifier> r = new List<DockerNotifier>();
            foreach (MountPoint mount in container.Mounts) {
                String hostDirectory = GetHostDirectory(mount.Source);
                if (!String.IsNullOrEmpty(hostDirectory)) {
                    r.Add(new DockerNotifier(container.ID, hostDirectory, mount.Destination, (e) => { r.Remove(e); }, m_Log, m_IsIgnorefileMandatory, m_NotifierAction));
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

        private void LogMessage(String message, EventLogEntryType info = EventLogEntryType.Information) {
            try { Console.WriteLine(message); } catch (Exception ex) { m_Log?.WriteEntry(ex.Message, EventLogEntryType.Error); }
            m_Log?.WriteEntry(message, info);
        }
    }
}
