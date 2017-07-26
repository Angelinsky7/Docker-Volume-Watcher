using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WPFLocalizeExtension.Extensions;

namespace ch.darkink.docker_volume_watcher.trayapp.Helpers {

    public static class Loc {

        /// <summary>
        /// Gets the localized value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static T Get<T>(String key) {
            return LocExtension.GetLocalizedValue<T>(Assembly.GetCallingAssembly().GetName().Name + ":Resources:" + key);
        }

        /// <summary>
        /// Gets the localized value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        public static T Get<T>(String key, String assembly) {
            return LocExtension.GetLocalizedValue<T>(assembly + ":Resources:" + key);
        }

        ///// <summary>
        ///// Gets the localized infrastructure value.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="key">The key.</param>
        ///// <returns></returns>
        //public static T GetCommons<T>(String key) {
        //    return LocExtension.GetLocalizedValue<T>(typeof(LocalizationProvider).Assembly.GetName().Name + ":Resources:" + key);
        //}

    }
}
