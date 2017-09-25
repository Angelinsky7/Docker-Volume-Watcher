using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public class OnlyBashNotifierActionType : BaseNotifierActionType {

        public OnlyBashNotifierActionType(DockerNotifier notifier) : base(notifier) { }

        protected override void Configure() {
            m_Shell = "bash";
        }
    }
}
