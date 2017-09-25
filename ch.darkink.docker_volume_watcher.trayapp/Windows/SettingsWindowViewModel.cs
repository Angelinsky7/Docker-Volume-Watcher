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
using ch.darkink.docker_volume_watcher.trayapp.Models;

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

        public IEnumerable<NotifierActionType> NotifierActionTypes { get; private set; }
       
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
            get { return RegistryService?.PollInterval ?? 1000; }
            set { RegistryService.PollInterval = value; }
        }
        public Int32 NotifierActionType {
            get { return RegistryService?.NotifierActionType ?? 1; }
            set { RegistryService.NotifierActionType = value; }
        }

        public Boolean IsIgnoreFileMandatory {
            get { return RegistryService?.IsIgnoreFileMandatory ?? false; }
            set { RegistryService.IsIgnoreFileMandatory = value; }
        }

        public SettingsWindowViewModel() {
            BuildNotifierActionTypes();

            //PropertyChanged += SettingsWindowViewModel_PropertyChanged;
            WireCommands();
        }

        private void BuildNotifierActionTypes() {
            Task<IEnumerable<NotifierActionType>>.Run(() => {
                return new List<NotifierActionType> {
                    new NotifierActionType{Id = 1, Caption = Loc.Get<String>("NotifierActionType_FirstShThenBash")},
                    new NotifierActionType{Id = 2, Caption = Loc.Get<String>("NotifierActionType_FirstBashThenSh")},
                    new NotifierActionType{Id = 3, Caption = Loc.Get<String>("NotifierActionType_OnlyBash")},
                    new NotifierActionType{Id = 4, Caption = Loc.Get<String>("NotifierActionType_OnlySh")},
                };
            }).ContinueWith((e) => {
                NotifierActionTypes = e.Result ?? new List<NotifierActionType>();
            }, TaskScheduler.FromCurrentSynchronizationContext());
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
            RaisePropertyChanged(nameof(IsIgnoreFileMandatory));
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
