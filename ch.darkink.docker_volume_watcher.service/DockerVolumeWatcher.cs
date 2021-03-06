﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher {
    public partial class DockerVolumeWatcher : ServiceBase {

        private DockerMonitor m_Monitor;

        public DockerVolumeWatcher() {
            InitializeComponent();
        }

        protected override void OnStart(String[] args) {
            eventLog.WriteEntry($"Service starting - v{typeof(DockerVolumeWatcher).Assembly.GetName().Version}, with polling interval as {args[0]}ms and mandatory ignore file : {args[1]}, notifier action : {args[2]}, docker endpoint : {args[3]}", EventLogEntryType.Information);

            Int32 polling = 1000;
            Boolean mandatory = true;
            Int32 notifierAction = 1;
            String dockerEndpoint = "npipe://./pipe/docker_engine";

            try {
                polling = Int32.Parse(args[0]);
                mandatory = Boolean.Parse(args[1]);
                notifierAction = Int32.Parse(args[2]);
                dockerEndpoint = args[3];
            } catch (Exception ex) {
                eventLog.WriteEntry($"Properties error : {ex.Message}", EventLogEntryType.Error);
            }

            try {
                m_Monitor = new DockerMonitor(eventLog, polling, mandatory, notifierAction, dockerEndpoint);
                m_Monitor.Start();
            } catch (Exception ex) {
                eventLog.WriteEntry($"Cannot start service : {ex.Message}", EventLogEntryType.Error);
            }
        }

        protected override void OnStop() {
            eventLog.WriteEntry($"Service stopping", EventLogEntryType.Information);

            if (m_Monitor != null) {
                m_Monitor.Stop();
                m_Monitor = null;
            }
        }
    }
}
