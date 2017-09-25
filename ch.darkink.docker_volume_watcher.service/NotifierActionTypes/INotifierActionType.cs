using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public interface INotifierActionType {

        Task Notify(String pathChanged);

    }
}
