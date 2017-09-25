using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public class FirstBashThenShNotifierActionType : SwitchNotifierActionType {

        public FirstBashThenShNotifierActionType(DockerNotifier notifier) : base(notifier) { }

        protected override void Configure() {
            base.Configure();

            m_Shell = "bash";
            m_ReplacingShell = "sh";
        }

    }
}
