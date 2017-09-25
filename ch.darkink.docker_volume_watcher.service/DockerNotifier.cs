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
using ch.darkink.docker_volume_watcher.service.NotifierActionTypes;

namespace ch.darkink.docker_volume_watcher {
    public class DockerNotifier {

        private const String FILE_DVWIGNORE_EXT = ".dvwignore";
        internal const String DOCKER_URI = "npipe://./pipe/docker_engine";

        internal String m_Container;
        internal String m_HostDirectory;
        internal String m_Destination;
        internal String m_IgnoreFile;

        private FileSystemWatcher m_Watcher;
        private Boolean m_IsDirectory;
        private Action<DockerNotifier> m_ReleaseNotifier;
        private EventLog m_Log;

        private IEnumerable<IgnorePathItem> m_IgnoreRules;
        private INotifierActionType m_NotifierAction;

        public DockerNotifier(String container, String hostDirectory, String destination, Action<DockerNotifier> releaseNotifier, EventLog log, Boolean ignoreFileMandatory, Int32 notifierAction) {
            if (String.IsNullOrEmpty(hostDirectory)) { throw new ArgumentNullException("hostDirectory"); }
            if (String.IsNullOrEmpty(destination)) { throw new ArgumentNullException("destination"); }

            m_Container = container ?? throw new ArgumentNullException("container");
            m_HostDirectory = hostDirectory;
            m_Destination = destination;
            m_ReleaseNotifier = releaseNotifier;
            m_Log = log;

            Boolean isIgnorefileExists = ComputeIgnoreFile();
            Boolean canWatch = !(ignoreFileMandatory && !isIgnorefileExists);
            if (canWatch) {
                WatchDirectory();
            } else {
                LogMessage($"{container} was ignore because there is no ingore file in {hostDirectory}");
            }

            m_NotifierAction = GetNotifierAction(notifierAction);
            LogMessage($"Notifier selected : {(m_NotifierAction?.GetType().ToString() ?? "No notifier selected")}");
        }

        private INotifierActionType GetNotifierAction(Int32 notifierAction) {
            switch (notifierAction) {
                case 1:
                    return new FirstShThenBashNotifierActionType(this);
                case 2:
                    return new FirstBashThenShNotifierActionType(this);
                case 3:
                    return new OnlyBashNotifierActionType(this);
                case 4:
                    return new OnlyShNotifierActionType(this);
            }
            return null;
        }

        private Boolean ComputeIgnoreFile() {
            Boolean result = true;

            if (Directory.Exists(m_HostDirectory)) {
                m_IgnoreFile = Path.Combine(m_HostDirectory, FILE_DVWIGNORE_EXT);
            }

            if (m_IgnoreFile != null) {
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
                } else {
                    result = false;
                }
            }

            return result;
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

            await m_NotifierAction?.Notify(pathChanged);
        }

        private Boolean IsInIgnorePath(String pathChanged) {
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

        internal String GetDockerDirectory(String source) {
            String relativePath = source.Replace(m_HostDirectory, "").Replace("\\", "/");
            return $"{m_Destination}{relativePath}";
        }

        private void M_Watcher_Changed(Object sender, FileSystemEventArgs e) {
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

        internal void LogMessage(String message) {
#if DEBUG
            try { Console.WriteLine(message); } catch (Exception ex) { m_Log?.WriteEntry(ex.Message, EventLogEntryType.Error); }
#endif
            m_Log?.WriteEntry(message, EventLogEntryType.Information);
        }

    }
}
