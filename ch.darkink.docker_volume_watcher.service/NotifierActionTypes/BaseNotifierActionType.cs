using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace ch.darkink.docker_volume_watcher.service.NotifierActionTypes {
    public abstract class BaseNotifierActionType : INotifierActionType {

        public DockerNotifier m_Notifier;

        protected String m_Shell;
        protected String m_ShellOptions = "-c";
        protected String m_ShellCommand = "chmod $(stat -c %a {0}) {0}";
        protected Boolean m_HandleError;

        public BaseNotifierActionType(DockerNotifier notifier) {
            m_Notifier = notifier;
            Configure();
        }

        protected abstract void Configure();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected virtual async Task PostProcess(String pathChanged, MultiplexedStream stream) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public virtual async Task Notify(String pathChanged) {

            using (DockerClient client = new DockerClientConfiguration(new Uri(DockerNotifier.DOCKER_URI)).CreateClient()) {
                String dockerPath = m_Notifier.GetDockerDirectory(pathChanged);

                ContainerExecCreateResponse response = await client.Containers.ExecCreateContainerAsync(m_Notifier.m_Container, new ContainerExecCreateParameters {
                    AttachStderr = false,
                    AttachStdin = false,
                    AttachStdout = m_HandleError,
                    Cmd = new String[] { m_Shell, m_ShellOptions, String.Format(m_ShellCommand, dockerPath) },
                    Detach = false,
                    Tty = false,
                    User = "root",
                    Privileged = true
                });

                using (var stream = await client.Containers.StartAndAttachContainerExecAsync(response.ID, false, default(CancellationToken))) {
                    if (m_HandleError) { await PostProcess(pathChanged, stream); }
                }

                m_Notifier.LogMessage($"Notify {pathChanged} mapped tp {dockerPath} has changed into {m_Notifier.m_Container}");
            }

        }

    }
}
