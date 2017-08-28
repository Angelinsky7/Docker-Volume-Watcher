using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System.IO;
using System.Text.RegularExpressions;
using Docker.DotNet;
using System.Threading;
using System.Diagnostics;
using ch.darkink.docker_volume_watcher.service;

namespace ch.darkink.docker_volume_watcher {
    public class DockerNotifier {

        private String m_Container;
        private String m_HostDirectory;
        private String m_Destination;
        private String m_IgnoreFile;

        private FileSystemWatcher m_Watcher;
        private Boolean m_IsDirectory;
        private Action<DockerNotifier> m_ReleaseNotifier;
        private EventLog m_Log;

        private IEnumerable<IgnorePathItem> m_IgnoreRules;

        public DockerNotifier(String container, String hostDirectory, String destination, Action<DockerNotifier> releaseNotifier, EventLog log) {
            if (String.IsNullOrEmpty(hostDirectory)) { throw new ArgumentNullException("hostDirectory"); }
            if (String.IsNullOrEmpty(destination)) { throw new ArgumentNullException("destination"); }

            m_Container = container ?? throw new ArgumentNullException("container");
            m_HostDirectory = hostDirectory;
            m_Destination = destination;
            m_ReleaseNotifier = releaseNotifier;
            m_Log = log;

            ComputeIgnoreFile();
            WatchDirectory();
        }

        private void ComputeIgnoreFile() {
            if (Directory.Exists(m_HostDirectory)) {
                m_IgnoreFile = Path.Combine(m_HostDirectory, ".dvwignore");
            }

            FileInfo fileInfo = new FileInfo(m_IgnoreFile);
            if (fileInfo.Exists) {
                List<IgnorePathItem> tests = new List<IgnorePathItem>();
                String[] lines = File.ReadAllLines(fileInfo.FullName);
                foreach (String line in lines) {
                    if (line.StartsWith("#") || String.IsNullOrEmpty(line.Trim())) { continue; }

                    String test = line;
                    IgnorePathItemType type;
                    Object custom = null;
                    if (line.StartsWith("*")) {
                        type = IgnorePathItemType.EndWith;
                        test = test.Replace("*", String.Empty);
                    } else if (line.EndsWith("*")) {
                        type = IgnorePathItemType.StartWith;
                        test = test.Replace("*", String.Empty);
                    } else {
                        type = IgnorePathItemType.Regex;
                        String regexPattern = line;
                        Int32 indexOfStar = regexPattern.IndexOf("*");
                        Char nextCharAfterStart = regexPattern[indexOfStar + 1];
                        regexPattern = regexPattern.Replace("*", $"[^{nextCharAfterStart}]+");
                        regexPattern = regexPattern.Replace("/", "\\/");
                        regexPattern = regexPattern.Replace(".", "\\.");
                        custom = new Regex(regexPattern);
                    }
                    tests.Add(new IgnorePathItem {
                        Test = test,
                        Type = type,
                        Custom = custom
                    });
                }
                m_IgnoreRules = tests;
            }
        }

        private void WatchDirectory() {
            if (Directory.Exists(m_HostDirectory)) {
                m_IsDirectory = true;
            } else if (File.Exists(m_HostDirectory)) {
                m_IsDirectory = false;
            } else {
                LogMessage("HostDirectory is not a valid file or directory");
                throw new ArgumentException("HostDirectory is not a valid file or directory");
            }
            if (m_IsDirectory) {
                m_Watcher = new FileSystemWatcher(m_HostDirectory) {
                    Filter = "*.*",
                    IncludeSubdirectories = true
                };
            } else {
                FileInfo FileInfo = new FileInfo(m_HostDirectory);
                m_Watcher = new FileSystemWatcher(FileInfo.Directory.FullName, FileInfo.Name);
            }
            m_Watcher.NotifyFilter = NotifyFilters.LastWrite;
            m_Watcher.Changed += M_Watcher_Changed;
            m_Watcher.EnableRaisingEvents = true;

            LogMessage($"Watcher create for {m_HostDirectory}");
        }

        private async Task Notify(String pathChanged) {
            if (IsInIgnorePath(pathChanged)) {
                LogMessage($"CheckPath {pathChanged} was excluded because of the ignore file");
                return;
            }

            var echo = Encoding.UTF8.GetBytes("ls -al");

            using (DockerClient client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient()) {
                String dockerPath = GetDockerDirectory(pathChanged);

                ContainerExecCreateResponse response = await client.Containers.ExecCreateContainerAsync(m_Container, new ContainerExecCreateParameters {
                    AttachStderr = false,
                    AttachStdin = false,
                    AttachStdout = false,
                    Cmd = new string[] { "bash", "-c", $"chmod $(stat -c %a {dockerPath}) {dockerPath}" },
                    Detach = false,
                    Tty = false,
                    User = "root",
                    Privileged = true
                });

                using (var stream = await client.Containers.StartAndAttachContainerExecAsync(response.ID, false, default(CancellationToken))) { }

                LogMessage($"Notify {pathChanged} mapped tp {dockerPath} has changed into {m_Container}");
            }
        }

        private bool IsInIgnorePath(String pathChanged) {
            if (m_IgnoreRules == null) { return false; }
            String relativePath = pathChanged.Replace(m_HostDirectory, "").Replace("\\", "/").Substring(1);
            foreach (IgnorePathItem test in m_IgnoreRules) {
                switch (test.Type) {
                    case IgnorePathItemType.StartWith:
                        if (relativePath.StartsWith(test.Test)) { return true; }
                        break;
                    case IgnorePathItemType.EndWith:
                        if (relativePath.EndsWith(test.Test)) { return true; }
                        break;
                    case IgnorePathItemType.Regex:
                        if (((Regex)test.Custom).IsMatch(relativePath)) { return true; }
                        break;
                }
            }
            return false;
        }

        private String GetDockerDirectory(String source) {
            String relativePath = source.Replace(m_HostDirectory, "").Replace("\\", "/");
            return $"{m_Destination}{relativePath}";
        }

        private void M_Watcher_Changed(object sender, FileSystemEventArgs e) {
            LogMessage($"File has changed : {e.ChangeType} - {e.FullPath} - {e.Name}");

            Notify(e.FullPath).ContinueWith((t) => {
                if (t.Exception != null && t.Exception.InnerException is DockerContainerNotFoundException) {
                    ReleaseContainer();
                }
            });
        }

        private void ReleaseContainer() {
            Release();
            m_ReleaseNotifier?.Invoke(this);
        }

        public void Release() {
            if (m_Watcher != null) {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Changed -= M_Watcher_Changed;
                m_Watcher.Dispose();
                m_Watcher = null;
            }
        }

        private void LogMessage(String message) {
            if (Console.IsOutputRedirected) { Console.WriteLine(message); }
            m_Log?.WriteEntry(message, EventLogEntryType.Information);
        }

    }
}
