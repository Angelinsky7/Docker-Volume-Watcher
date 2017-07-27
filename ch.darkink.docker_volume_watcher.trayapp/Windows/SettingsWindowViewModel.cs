using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using ch.darkink.docker_volume_watcher.trayapp.Services;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp.Windows {

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SettingsWindowViewModel : BindableBase, IPartImportsSatisfiedNotification {

        [Import]
        public ServiceMonitor ServiceMonitor { get; set; }
        [Import]
        public UpdateMonitor UpdateMonitor { get; set; }
        [Import]
        public RegistryService RegistryService { get; set; }

        public Nullable<Boolean> IsServiceRunning { get; private set; }
        public String ServiceState { get; private set; }
        public Boolean IsLoading { get { return IsServiceRunning ?? false; } }
      
        public DelegateCommand ApplyCommand { get; private set; }
        public DelegateCommand ResetCommand { get; private set; }

        public Boolean IsStartAutoService {
            get { return RegistryService?.IsStartAutoService ?? false; }
            set { RegistryService.IsStartAutoService = value; }
        }
        public Boolean IsCheckAuto {
            get { return RegistryService?.CheckAuto ?? false; }
            set {
                RegistryService.CheckAuto = value;
                UpdateMonitor.CheckAutoHasChanged(value);
            }
        }
        public Int32 PollingInterval {
            get { return RegistryService?.PollInterval ?? 500; }
            set { RegistryService.PollInterval = value; }
        }

        public SettingsWindowViewModel() {
            //PropertyChanged += SettingsWindowViewModel_PropertyChanged;
            WireCommands();
        }

        private void WireCommands() {
            ResetCommand = new DelegateCommand(OnResetCommand);
            ApplyCommand = new DelegateCommand(OnApplyCommand);
        }

        #region ResetCommand

        private void OnResetCommand() {
            Task.Run(() => {
                ServiceMonitor.Stop();
                ServiceMonitor.Start();
            });
        }

        #endregion

        #region ApplyCommand

        private void OnApplyCommand() {
            RefreshProperties();
            ResetCommand.Execute();
        }

        #endregion

        public void OnImportsSatisfied() {
            RefreshProperties();
            IsServiceRunning = ServiceMonitor.Status;
            ServiceState = ServiceMonitor.OperationAsString;
            ServiceMonitor.PropertyChanged += ServiceMonitor_PropertyChanged;
            RaisePropertyChanged(nameof(IsLoading));
        }

        private void RefreshProperties() {
            RaisePropertyChanged(nameof(IsStartAutoService));
            RaisePropertyChanged(nameof(IsCheckAuto));
            RaisePropertyChanged(nameof(PollingInterval));
        }

        private void ServiceMonitor_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(ServiceMonitor.Status):
                case nameof(ServiceMonitor.Operation):
                    IsServiceRunning = ServiceMonitor.Status;
                    ServiceState = ServiceMonitor.OperationAsString;
                    RaisePropertyChanged(nameof(IsLoading));
                    break;
            }
        }

    }
}
