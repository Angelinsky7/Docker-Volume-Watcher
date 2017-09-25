using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public abstract class SwitchNotifierActionType : BaseNotifierActionType {

        protected String m_ReplacingShell;

        public SwitchNotifierActionType(DockerNotifier notifier) : base(notifier) { }

        protected override void Configure() {
            m_HandleError = true;
        }

        protected override async Task PostProcess(String pathChanged, MultiplexedStream stream) {
            MemoryStream output = new MemoryStream();
            await stream.CopyOutputToAsync(null, output, null, default(CancellationToken));
            output.Position = 0;
            using (StreamReader reader = new StreamReader(output)) {
                String text = await reader.ReadToEndAsync();
                if (text.Contains($"exec: \\\"{m_Shell}\\\": executable file not found in $PATH")) {
                    m_Notifier.LogMessage($"Cannot execute {m_Shell} for this container ({m_Notifier.m_Container}) changing for {m_ReplacingShell}");
                    m_Shell = m_ReplacingShell;
                    m_HandleError = false;
                    await Notify(pathChanged);
                } else {
                    m_Notifier.LogMessage($"Can execute {m_Shell} for this container ({m_Notifier.m_Container}) disactivating error handling");
                    m_HandleError = false;
                }
            }
        }

    }
}
