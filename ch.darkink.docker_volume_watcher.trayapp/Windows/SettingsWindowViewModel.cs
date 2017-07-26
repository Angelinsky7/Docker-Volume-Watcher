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

        private const String REGISTRY_RUN = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        
        [Import]
        public ServiceMonitor ServiceMonitor { get; set; }
        [Import]
        public UpdateMonitor UpdateMonitor { get; set;}

        public Nullable<Boolean> IsServiceRunning { get; private set; }
        public String ServiceState { get; private set; }
        public Boolean IsStartAutoService { get; set; }
        public Boolean IsCheckAuto { get; set; }
        public Boolean IsLoading { get { return IsServiceRunning ?? false; } }

        public DelegateCommand ResetCommand { get; private set; }

        public SettingsWindowViewModel() {
            PropertyChanged += SettingsWindowViewModel_PropertyChanged;
            WireCommands();
        }

        private void SettingsWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(IsStartAutoService):
                    UpdateAuoStartKeyRegistry();
                    break;
                case nameof(IsCheckAuto):
                    UpdateCheckAutoKeyRegistry();
                    break;
            }
        }

        #region AutoStart

        private Boolean IsAutoStartKeyRegistered() {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            String assemlyTitle = curAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            return key.GetValue(assemlyTitle) != null;
        }

        private void UpdateAuoStartKeyRegistry() {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true);
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            String assemlyTitle = curAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            if (IsStartAutoService) {
                key.SetValue(assemlyTitle, $"\"{curAssembly.Location}\"");
            } else {
                key.DeleteValue(assemlyTitle);
            }
        }

        #endregion

        #region CheckAuto

        private Boolean GetCheckAutoKeyRegistered() {
            return UpdateMonitor.GetCheckAutoKeyRegistered();
        }

        private void UpdateCheckAutoKeyRegistry() {
            UpdateMonitor.UpdateCheckAutoKeyRegistry(IsCheckAuto);
        }

        #endregion

        private void WireCommands() {
            ResetCommand = new DelegateCommand(OnResetCommand);
        }

        #region ResetCommand

        private void OnResetCommand() {
            Task.Run(() => {
                ServiceMonitor.Stop();
                ServiceMonitor.Start();
            });
        }

        #endregion

        public void OnImportsSatisfied() {
            IsStartAutoService = IsAutoStartKeyRegistered();
            IsCheckAuto = GetCheckAutoKeyRegistered();

            IsServiceRunning = ServiceMonitor.Status;
            ServiceState = ServiceMonitor.OperationAsString;
            ServiceMonitor.PropertyChanged += ServiceMonitor_PropertyChanged;
            RaisePropertyChanged(nameof(IsLoading));
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
