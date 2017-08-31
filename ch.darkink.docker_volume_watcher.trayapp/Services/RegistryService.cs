using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.trayapp.Services {

    [Export]
    public class RegistryService {

        private const String REGISTRY_RUN = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const String REGISTRY_SETTINGS = "SOFTWARE\\Darkink\\ch.darkink.docker_volume_watcher\\Settings";
        private const String REGISTRY_SETTINGS_CHECKUPDATE = "CheckUpdateAutomatically";
        private const String REGISTRY_SETTINGS_POLLINTERVAL = "PollInterval";
        private const String REGISTRY_SETTINGS_IGNOREFILE_MANDATORY = "IngoreFileMandatory";

        private String AssemblyTitle {
            get {
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                return curAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            }
        }
        private String AssemblyLocation {
            get {
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        public Boolean CheckAuto {
            get { return GetValueFromRegistry(REGISTRY_SETTINGS_CHECKUPDATE, 0) != 0; }
            set { SetValueToRegistry(REGISTRY_SETTINGS_CHECKUPDATE, value ? 1 : 0); }
        }

        public Boolean IsStartAutoService {
            get { return GetValueFromRegistry(AssemblyTitle, String.Empty, REGISTRY_RUN) != null; }
            set {
                if (value) {
                    SetValueToRegistry(AssemblyTitle, $"\"{AssemblyLocation}\"", REGISTRY_RUN);
                } else {
                    DelValueToRegistry(AssemblyTitle, REGISTRY_RUN);
                }
            }
        }

        public Int32 PollInterval {
            get { return GetValueFromRegistry(REGISTRY_SETTINGS_POLLINTERVAL, 500); }
            set { SetValueToRegistry(REGISTRY_SETTINGS_POLLINTERVAL, value); }
        }

        public Boolean IsIgnoreFileMandatory {
            get { return GetValueFromRegistry(REGISTRY_SETTINGS_IGNOREFILE_MANDATORY, 0) != 0; }
            set { SetValueToRegistry(REGISTRY_SETTINGS_IGNOREFILE_MANDATORY, value ? 1 : 0); }
        }

        private T GetValueFromRegistry<T>(String keyName, T defaultValue = default(T), String opensubKey = REGISTRY_SETTINGS) {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(opensubKey, true);
            return (T)key.GetValue(keyName, defaultValue);
        }
         
        private void SetValueToRegistry<T>(String keyName, T newValue, String opensubKey = REGISTRY_SETTINGS) {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(opensubKey, true);
            key.SetValue(keyName, newValue);
        }

        private void DelValueToRegistry(String keyName, String opensubKey = REGISTRY_SETTINGS) {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(opensubKey, true);
            key.DeleteValue(keyName);
        }

    }
}