using ch.darkink.docker_volume_watcher.trayapp.Notify;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp.Services {

    public interface ITaskTrayService {

        NotifyIconViewModel Notify { get; }

        void Dispose();
    }
}
