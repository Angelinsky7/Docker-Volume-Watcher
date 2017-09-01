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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;

namespace ch.darkink.docker_volume_watcher.trayapp.Services {

    [Export]
    public class UpdateMonitor : BindableBase {

        [Import]
        public RegistryService RegistryService { get; set; }

        [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
        public static extern bool StrongNameSignatureVerificationEx(string wszFilePath, bool fForceVerification, ref bool pfWasVerified);

        public static bool CheckToken(string assembly, byte[] expectedToken) {
            if (assembly == null) { throw new ArgumentNullException("assembly"); }
            if (expectedToken == null) { throw new ArgumentNullException("expectedToken"); }

            try {
                Assembly asm = Assembly.LoadFrom(assembly);
                byte[] asmToken = asm.GetName().GetPublicKey();
                if (asmToken.Length != expectedToken.Length) { return false; }

                for (int i = 0; i < asmToken.Length; i++) {
                    if (asmToken[i] != expectedToken[i]) {
                        return false;
                    }
                }
                return true;
            } catch (System.IO.FileNotFoundException) {
                return false;
            } catch (BadImageFormatException) {
                return false;
            }
        }

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
                FileInfo assemlyFileInfo = new FileInfo(typeof(UpdateMonitor).Assembly.Location);
                String location = assemlyFileInfo.Directory.FullName;
                String updater = Path.Combine(location, "ch.darkink.docker_volume_watcher.updater.exe");
                Int32 pidAppTray = Process.GetCurrentProcess().Id;

                if (!CheckUpdater(updater)) { MessageBox.Show("Updater is not valid"); }

                ProcessStartInfo Info = new ProcessStartInfo() {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = updater,
                    Arguments = $"\"{newVersionMSIOrErrorMessage}\" \"{location}\" {pidAppTray}"
                };
                Process.Start(Info);
                NotifyCommandManager.Quit.Execute();
            } else {
                MessageBox.Show(newVersionMSIOrErrorMessage);
            }
        }

        private Boolean CheckUpdater(String updater) {
            bool notForced = false;
            bool verified = StrongNameSignatureVerificationEx(updater, false, ref notForced);
            byte[] token = new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xD9, 0x03, 0x97, 0x2E, 0x95, 0x7C, 0xCA, 0x1E, 0x7B, 0x38, 0x49, 0x9B, 0xF0, 0x30, 0x48, 0x75, 0xFE, 0x76, 0x41, 0x3F, 0x3B, 0xB6, 0x84, 0x62, 0x95, 0x90, 0x19, 0xE8, 0x16, 0x37, 0x4E, 0xE1, 0xD9, 0x05, 0x6D, 0x24, 0x42, 0x38, 0xFB, 0xEB, 0xC6, 0x3B, 0x92, 0x09, 0x4A, 0x02, 0xE4, 0x4F, 0x7B, 0xF8, 0xD3, 0x3D, 0xC4, 0x28, 0x59, 0x90, 0x38, 0xF9, 0x3D, 0x6A, 0x11, 0x1E, 0x94, 0x61, 0x03, 0x57, 0x35, 0xD9, 0x3D, 0xDB, 0x24, 0x55, 0xE9, 0x46, 0xB6, 0x5E, 0x74, 0x9E, 0x29, 0xEE, 0x77, 0x89, 0x5D, 0x9A, 0x70, 0x63, 0xB1, 0x09, 0x2C, 0x13, 0xF8, 0xE1, 0x64, 0x3C, 0x14, 0xCB, 0xB2, 0xDC, 0x7B, 0xB8, 0xFE, 0x55, 0x11, 0x1E, 0xBA, 0xFF, 0xB4, 0x4B, 0x03, 0x3F, 0x2C, 0xF1, 0xF7, 0x68, 0x37, 0x7E, 0xF9, 0x5E, 0xC5, 0x8D, 0xD7, 0x48, 0xF2, 0x66, 0x23, 0xAE, 0xB2, 0xB3 };
            return (CheckToken(updater, token) && verified && notForced);
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
