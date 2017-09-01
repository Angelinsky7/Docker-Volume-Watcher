using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.updater {
    class Program {
        static void Main(string[] args) {

            if (args.Length != 3) { Environment.Exit(3); return; }

            String msi = args[0];
            String location = args[1];
            Int32 pid = Int32.Parse(args[2]);

            if (pid == 0) { Environment.Exit(100); return; }
            if (!File.Exists(msi)) { Environment.Exit(404); return; }

            Process apptray = Process.GetProcessById(pid);
            String appNameFilename = apptray.MainModule.FileName;
            Boolean apptrayAsExited = apptray.WaitForExit(60000);

            if (!apptrayAsExited) { Environment.Exit(500); return; }

            ProcessStartInfo installerInfo = new ProcessStartInfo() {
                Arguments = $"/i {msi}",
                UseShellExecute = true,
                FileName = "msiexec",
                Verb = "runas"
            };

            Boolean installerAsExited = false;
            try {
                Process installer = Process.Start(installerInfo);
                installerAsExited = installer.WaitForExit(60000);
            } catch (Win32Exception ex) {
                if (ex.NativeErrorCode == 1223) { Environment.Exit(1223); return; }
            }

            if (installerAsExited && File.Exists(appNameFilename)) {
                Process.Start(new ProcessStartInfo() {
                    Arguments = $"/C del /Q /F {msi}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd",
                });
                Process.Start(appNameFilename);
            }

        }

    }
}
