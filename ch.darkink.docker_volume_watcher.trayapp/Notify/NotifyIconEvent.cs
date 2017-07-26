using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp.Notify {
    public class NotifyIconEventArgs : EventArgs {

        public String Title { get; set; }
        public String Message { get; set; }
        public BalloonIcon Info { get; set; }

    }
}
