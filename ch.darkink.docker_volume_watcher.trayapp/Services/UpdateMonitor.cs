using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using ch.darkink.docker_volume_watcher.trayapp.Notify;
using Microsoft.Win32;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;

namespace ch.darkink.docker_volume_watcher.trayapp.Services {

    [Export]
    public class UpdateMonitor : BindableBase {

        [Import]
        public RegistryService RegistryService { get; set; }

        private const String UPDATE_URL = "https://raw.githubusercontent.com/Angelinsky7/Docker-Volume-Watcher/master/version?dummy={0}";
        private const String NEWFILE_URL_FORMAT = "https://github.com/Angelinsky7/Docker-Volume-Watcher/releases/download/{0}/ch.darkink.docker_volume_watcher.msi";
        private const Double POLLING_INTERVAL = 3600000;
        private Random m_Rnd;
        private Timer m_Timer;
        private Boolean m_InCheckUpdate;

        public UpdateMonitor() {
            m_Rnd = new Random();
            m_Rnd.Next();
        }

        public Tuple<Boolean, String, Version> CheckVersion(Version version) {
            Tuple<String, Boolean> file = GetFileVersion();
            if (file.Item2) {
                if (!String.IsNullOrEmpty(file.Item1)) {
                    Version newVersion = GetVersionFromFile(file.Item1);
                    if (newVersion != null) {
                        return new Tuple<Boolean, String, Version>(newVersion > version, null, newVersion);
                    }
                }
            }
            return new Tuple<Boolean, String, Version>(false, file.Item1, null);
        }

        public void Start() {
            if (RegistryService.CheckAuto) { StartPolling(); }
        }

        public void RestartApplicationAndInstall(Version version) {
            String newVersionMSIOrErrorMessage = DownloadNewVersionFromSource(version);
            if (File.Exists(newVersionMSIOrErrorMessage)) {
                ProcessStartInfo Info = new ProcessStartInfo() {
                    Arguments = $"/C {newVersionMSIOrErrorMessage} && del /Q /F {newVersionMSIOrErrorMessage} && {typeof(UpdateMonitor).Assembly.Location}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd"
                };
                Process.Start(Info);
                NotifyCommandManager.Quit.Execute();
            } else {
                MessageBox.Show(newVersionMSIOrErrorMessage);
            }
        }

        public void CheckAutoHasChanged(Boolean newValue) {
            if (newValue) {
                StartPolling();
            } else {
                StopPolling();
            }
        }

        private String DownloadNewVersionFromSource(Version version) {
            String r = $"{Path.GetTempFileName()}.msi";

            try {
                using (WebClient webClient = new WebClient()) {
                    webClient.DownloadFile(String.Format(NEWFILE_URL_FORMAT, version), r);
                }
            } catch (Exception ex) {
                r = ex.Message;
            }

            return r;
        }

        private Version GetVersionFromFile(String file) {
            using (StringReader reader = new StringReader(file)) {
                String line = string.Empty;
                do {
                    line = reader.ReadLine();
                    if (line != null) {
                        if (line[0] == '#') {

                        } else {
                            Version v = new Version(line);
                            return v;
                        }
                    }

                } while (line != null);
            }

            return null;
        }

        private Tuple<String, Boolean> GetFileVersion() {
            String r = String.Empty;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format(UPDATE_URL, m_Rnd.Next(4000030, 504230420)));
                request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    using (Stream stream = response.GetResponseStream()) {
                        using (StreamReader reader = new StreamReader(stream)) {
                            r = reader.ReadToEnd();
                        }
                    }
                }
            } catch (Exception ex) {
                r = ex.Message;
            }

            return new Tuple<String, Boolean>(r, true);
        }

        private void StartPolling() {
            StopPolling();

            m_Timer = new System.Timers.Timer() {
                Interval = POLLING_INTERVAL,
            };
            m_Timer.Elapsed += M_Timer_Elapsed;
            m_Timer.Start();
        }

        private void M_Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (!m_InCheckUpdate) {
                m_InCheckUpdate = true;
                try {
                    System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(() => {
                        NotifyCommandManager.CheckUpdate.Execute(new UpdateConfigCheck());
                    }));
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                    thread = null;
                } catch (Exception ex) { Console.WriteLine(ex.Message); }
                m_InCheckUpdate = false;
            }
        }

        private void StopPolling() {
            if (m_Timer != null) {
                m_Timer.Elapsed -= M_Timer_Elapsed;
                m_Timer.Stop();
                m_Timer.Dispose();
                m_Timer = null;
            }
        }

    }
}
