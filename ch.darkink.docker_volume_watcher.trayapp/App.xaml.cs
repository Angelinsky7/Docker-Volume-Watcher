using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Practices.ServiceLocation;
using ch.darkink.docker_volume_watcher.trayapp.Notify;
using ch.darkink.docker_volume_watcher.trayapp.Prism;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ch.darkink.docker_volume_watcher.trayapp.Services;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace ch.darkink.docker_volume_watcher.trayapp {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ITaskTrayService {

        public TaskbarIcon Taskbar { get; private set; }

        public NotifyIconViewModel Notify => Taskbar.DataContext as NotifyIconViewModel;

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            try {
                Bootstrap bootstrapper = new Bootstrap();
                bootstrapper.Run();
                ApplicationStarted();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
                Application.Current.Shutdown();
            }
        }

        public void ApplicationStarted() {
            WPFLocalizeExtension.Providers.ResxLocalizationProvider.Instance.FallbackAssembly = "ch.darkink.docker_volume_watcher.trayapp";
            WPFLocalizeExtension.Providers.ResxLocalizationProvider.Instance.FallbackDictionary = "Resources";

            Taskbar = (TaskbarIcon)FindResource("taskbarIcon");
            NotifyIconViewModel dc = ServiceLocator.Current.GetInstance<NotifyIconViewModel>();
            dc.NotifyIconEvent += (s, e) => { Taskbar.ShowBalloonTip(e.Title, e.Message, e.Info); };
            Taskbar.DataContext = dc;
            
            ServiceLocator.Current.GetInstance<CompositionContainer>().ComposeExportedValue<ITaskTrayService>(this);
            ServiceLocator.Current.GetInstance<UpdateMonitor>().Start();
            Task.Run(() => {
                ServiceLocator.Current.GetInstance<ServiceMonitor>().Start();
            });
        }

        public void Dispose() {
            if (Notify != null) {
                Notify.Dispose();
            }
            if (Taskbar != null) {
                Taskbar.Dispose();
                Taskbar = null;
            }
        }

    }
}
