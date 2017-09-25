using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using Prism.Mvvm;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ch.darkink.docker_volume_watcher.trayapp.Services {

    [Export]
    public class ServiceMonitor : BindableBase {

        [Import]
        public RegistryService RegistryService { get; set; }

        public const String SERVICENAME = "Docker Volume Watcher";

        [AlsoNotifyFor(nameof(OperationAsString))]
        public ServiceMonitorOperation Operation { get; private set; }

        public String ErrorMessage { get; private set; }

        private Nullable<Boolean> m_OldStatus;
        public Nullable<Boolean> Status {
            get {
                if (!IsServiceValid) { return false; }
                m_ServiceMonitor.Refresh();

                switch (m_ServiceMonitor.Status) {
                    case ServiceControllerStatus.Running:
                        return true;
                    case ServiceControllerStatus.Stopped:
                        return false;
                }
                return null;
            }
        }

        public String OperationAsString {
            get {
                if (m_ServiceMonitor == null) { return Loc.Get<String>("ServiceNotRunningLabel"); }
                switch (Operation) {
                    case ServiceMonitorOperation.Running:
                        return Loc.Get<String>("ServiceRunningLabel");
                    case ServiceMonitorOperation.Starting:
                        return Loc.Get<String>("ServiceStartingLabel");
                    case ServiceMonitorOperation.Stopping:
                        return Loc.Get<String>("ServiceStoppingLabel");
                    case ServiceMonitorOperation.Error:
                        return Loc.Get<String>("ServiceErrorLabel");
                }
                return Loc.Get<String>("ServiceNotRunningLabel");
            }
        }

        public Boolean IsServiceValid {
            get {
                if (m_ServiceMonitor == null) { return false; }
                try {
                    return !String.IsNullOrEmpty(m_ServiceMonitor.DisplayName);
                } catch (Exception) { }
                return false;
            }
        }

        private ServiceController m_ServiceMonitor;
        private Timer m_Timer;

        public ServiceMonitor() {
            try {
                m_ServiceMonitor = new ServiceController(SERVICENAME);
            } catch (Exception ex) {
                ErrorMessage = ex.Message;
                Operation = ServiceMonitorOperation.Error;
            }

            Poll();
        }

        public void Start() {
            if (!IsServiceValid) { return; }

            if (Status != true) {
                m_ServiceMonitor.Start(new String[] {
                    RegistryService.PollInterval.ToString(),
                    RegistryService.IsIgnoreFileMandatory.ToString(),
                    RegistryService.NotifierActionType.ToString()
                });
                Operation = ServiceMonitorOperation.Starting;
                try {
                    m_ServiceMonitor.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    Operation = ServiceMonitorOperation.Running;
                } catch (System.ServiceProcess.TimeoutException ex) {
                    ErrorMessage = ex.Message;
                    Operation = ServiceMonitorOperation.Error;
                }
            } else if (Status == true) {
                Operation = ServiceMonitorOperation.Running;
            }
        }

        public void Stop() {
            if (!IsServiceValid) { return; }

            if (Status == true) {
                m_ServiceMonitor.Stop();
                Operation = ServiceMonitorOperation.Stopping;
                try {
                    m_ServiceMonitor.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    Operation = ServiceMonitorOperation.Stopped;
                } catch (System.ServiceProcess.TimeoutException ex) {
                    ErrorMessage = ex.Message;
                    Operation = ServiceMonitorOperation.Error;
                }
            }
        }

        private void Poll() {
            m_Timer = new Timer(500);
            m_Timer.Elapsed += M_Timer_Elapsed;
            m_Timer.Start();
        }

        private void M_Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (m_OldStatus != Status) {
                RaisePropertyChanged(nameof(Status));
                m_OldStatus = Status;
            }
        }

        public void Release() {
            if (m_ServiceMonitor != null) {
                m_ServiceMonitor.Dispose();
                m_ServiceMonitor = null;
            }

            if (m_Timer != null) {
                m_Timer.Stop();
                m_Timer.Dispose();
                m_Timer = null;
            }
        }

    }
}
