using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ch.darkink.docker_volume_watcher.trayapp.Helpers {
    public static class DummyWindow {

        public class DummyWindowIntance : Window, IDisposable {
            public void Dispose() {
                s_Window.Close();
                s_Window = null;
            }
        }

        private static DummyWindowIntance s_Window;

        public static DummyWindowIntance Create() {
            s_Window = new DummyWindowIntance() {
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStyle = WindowStyle.None,
                Top = 0,
                Left = 0,
                Width = 1,
                Height = 1,
                ShowInTaskbar = false
            };

            s_Window.Show();
            return s_Window;
        }

    }
}
