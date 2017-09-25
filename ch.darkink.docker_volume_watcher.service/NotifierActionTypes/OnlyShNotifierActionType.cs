using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public class OnlyShNotifierActionType : BaseNotifierActionType {

        public OnlyShNotifierActionType(DockerNotifier notifier) : base(notifier) { }

        protected override void Configure() {
            m_Shell = "sh";
        }
    }
}
