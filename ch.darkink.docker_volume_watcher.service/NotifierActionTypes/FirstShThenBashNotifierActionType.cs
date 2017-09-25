using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public class FirstShThenBashNotifierActionType : SwitchNotifierActionType {

        public FirstShThenBashNotifierActionType(DockerNotifier notifier) : base(notifier) { }

        protected override void Configure() {
            base.Configure();

            m_Shell = "sh";
            m_ReplacingShell = "bash";
        }

    }
}
