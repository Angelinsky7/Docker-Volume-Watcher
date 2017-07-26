using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using ch.darkink.docker_volume_watcher.trayapp.Properties;
using ch.darkink.docker_volume_watcher.trayapp.Services;
using Hardcodet.Wpf.TaskbarNotification;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ch.darkink.docker_volume_watcher.trayapp.Notify {

    [Export]
    public class NotifyIconViewModel : BindableBase, IPartImportsSatisfiedNotification {

        public event EventHandler<NotifyIconEventArgs> NotifyIconEvent;

        [Import]
        public ServiceMonitor ServiceMonitor { get; set; }
      
        public String Tooltip { get; set; }
        public Icon Icon { get; set; }

        private IconAnimation m_Animation;

        public NotifyIconViewModel() {
            m_Animation = new IconAnimation(Resources.startup_icon_animation, 64, 64);
            m_Animation.PropertyChanged += M_Animation_PropertyChanged;
            Icon = m_Animation.GetIcon(0);            
        }

        public void Start() {
            Tooltip = Loc.Get<String>("StartingLabel");
            m_Animation.Start();
        }
        public void StartReverse() {
            Tooltip = Loc.Get<String>("StoppingLabel");
            m_Animation.Reverse = true;
            m_Animation.Start();
        }
        public void Stop() {
            m_Animation.Stop();
        }

        private void M_Animation_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(IconAnimation.Current)) {
                Icon = m_Animation?.Current;
            }
        }

        public void Dispose() {
            if (m_Animation != null) {
                Stop();
                m_Animation.Dispose();
                m_Animation = null;
            }
        }

        public void ShowBalloonTip(String title, String message, BalloonIcon info) {
            NotifyIconEvent?.Invoke(this, new NotifyIconEventArgs {
                Title = title,
                Message = message,
                Info = info
            });
        }

        public void OnImportsSatisfied() {
            ServiceMonitor.PropertyChanged += ServiceMonitor_PropertyChanged;
        }

        private void ServiceMonitor_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(ServiceMonitor.Operation):
                    switch (ServiceMonitor.Operation) {
                        case ServiceMonitorOperation.Starting:
                            Start();
                            break;
                        case ServiceMonitorOperation.Stopping:
                            StartReverse();
                            break;
                        case ServiceMonitorOperation.Running:
                            Stop();
                            Icon = Properties.Resources.icon; 
                            Tooltip = Loc.Get<String>("TitleLabel");
                            ShowBalloonTip(Loc.Get<String>("TitleLabel"), Loc.Get<String>("ServiceRunningLabel"), BalloonIcon.Info);
                            break;
                    }
                    break;
            }
        }
    }
}
