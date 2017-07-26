using ch.darkink.docker_volume_watcher.trayapp.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp.Windows {

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AboutWindowViewModel : BindableBase {

        public String VersionInfo { get; private set; }
        public String ChannelInfo { get; private set; }

        public AboutWindowViewModel() {
            Assembly currentAssembly = typeof(AboutWindowViewModel).Assembly;
            VersionInfo = $"{Loc.Get<String>("VersionLabel")} {currentAssembly.GetName().Version.ToString()}";
            ChannelInfo = $"{Loc.Get<String>("ChannelLabel")}: {Loc.Get<String>("StableLabel")}";
        }

    }
}
