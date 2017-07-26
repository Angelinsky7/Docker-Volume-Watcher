using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ch.darkink.docker_volume_watcher.trayapp.Behaviors {
    public static class TaskbarIconBehaviors {

        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(Icon),
            typeof(TaskbarIconBehaviors),
            new PropertyMetadata(null, IconPropertyChanged)
        );

        public static Icon GetIcon(DependencyObject target) {
            return (Icon)target.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject target, Icon value) {
            target.SetValue(IconProperty, value);
        }

        private static void IconPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) {
            if (target is TaskbarIcon control) {
                control.Icon = (Icon)e.NewValue;
            }
        }

    }
}
