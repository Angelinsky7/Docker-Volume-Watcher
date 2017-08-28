using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using ch.darkink.docker_volume_watcher.trayapp.Properties;
using ch.darkink.docker_volume_watcher.trayapp.Services;
using ch.darkink.docker_volume_watcher.trayapp.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ch.darkink.docker_volume_watcher.trayapp.Helpers.DummyWindow;

namespace ch.darkink.docker_volume_watcher.trayapp.Notify {

    public class NotifyCommandManager {

        protected static ITaskTrayService m_TaskTrayService;
        protected static ITaskTrayService TaskTrayService {
            get {
                if (m_TaskTrayService == null) {
                    m_TaskTrayService = ServiceLocator.Current.GetInstance<ITaskTrayService>();
                }
                return m_TaskTrayService;
            }
        }

        public static DelegateCommand About { get; private set; }
        public static DelegateCommand Settings { get; private set; }
        public static DelegateCommand<UpdateConfigCheck> CheckUpdate { get; private set; }
        public static DelegateCommand Quit { get; private set; }

        static NotifyCommandManager() {
            WireCommands();
        }

        private static void WireCommands() {
            About = new DelegateCommand(OnAbout);
            Settings = new DelegateCommand(OnSettings);
            CheckUpdate = new DelegateCommand<UpdateConfigCheck>(OnCheckUpdate);
            Quit = new DelegateCommand(OnQuit);
        }

        private static void OnQuit() {
            TaskTrayService.Notify.StartReverse();

            Task.Run(() => {
                ServiceLocator.Current.GetInstance<ServiceMonitor>().Stop();
                ServiceLocator.Current.GetInstance<ServiceMonitor>().Release();
            }).ContinueWith((f) => {
                ServiceLocator.Current.GetInstance<NotifyIconViewModel>().ShowBalloonTip(Loc.Get<String>("TitleLabel"), Loc.Get<String>("ServiceStoppedLabel"), BalloonIcon.Info);
                TaskTrayService.Dispose();
                Application.Current.Shutdown();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static void OnAbout() {
            AboutWindowView about = ServiceLocator.Current.GetInstance<AboutWindowView>();
            about.Show();
        }

        private static void OnSettings() {
            SettingsWindowView settings = ServiceLocator.Current.GetInstance<SettingsWindowView>();
            settings.Show();
        }

        private static void OnCheckUpdate(UpdateConfigCheck sendMessageOption) {
            UpdateConfigCheck options = sendMessageOption ?? new UpdateConfigCheck { ShowMessage = true, ShowTooltip = true };

            UpdateMonitor update = ServiceLocator.Current.GetInstance<UpdateMonitor>();
            Tuple<Boolean, String, Version> result = update.CheckVersion(typeof(NotifyCommandManager).Assembly.GetName().Version);
            if (result.Item1) {
                if (options.ShowMessage) {
                    MessageBoxResult r = MessageBoxResult.None;
                    using (DummyWindowIntance dummy = DummyWindow.Create()) {
                        r = MessageBox.Show(dummy, Loc.Get<String>("NewVersionText"), Loc.Get<String>("NewVersionTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                    }
                    if (r == MessageBoxResult.Yes) {
                        Task.Run(() => {
                            update.RestartApplicationAndInstall(result.Item3);
                        });
                    }
                } else {
                    ServiceLocator.Current.GetInstance<NotifyIconViewModel>().ShowBalloonTip(Loc.Get<String>("NewVersionTitle"), Loc.Get<String>("NewVersionText"), BalloonIcon.Info);
                }
            } else {
                if (String.IsNullOrEmpty(result.Item2)) {
                    if (options.ShowTooltip) {
                        ServiceLocator.Current.GetInstance<NotifyIconViewModel>().ShowBalloonTip(Loc.Get<String>("NewVersionTitle"), Loc.Get<String>("UpToDateText"), BalloonIcon.Info);
                    }
                } else {
                    if (options.ShowMessage) {
                        using (DummyWindowIntance dummy = DummyWindow.Create()) {
                            MessageBox.Show(dummy, result.Item2, Loc.Get<String>("NewVersionTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    } else if (options.ShowTooltip) {
                        ServiceLocator.Current.GetInstance<NotifyIconViewModel>().ShowBalloonTip(Loc.Get<String>("NewVersionTitle"), result.Item2, BalloonIcon.Info);
                    }
                }
            }
        }

    }
}
