using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp {
    static class Program {

        internal class ExternalCalls {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (Mutex mutex = new Mutex(true, "ch.darkink.docker_volume_watcher.trayapp", out Boolean notRunning)) {
                if (notRunning) {
                    App app = new App();
                    app.InitializeComponent();
                    app.Run();
                }
            }

        }

    }
}
