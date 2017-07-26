using Microsoft.Practices.ServiceLocation;
using ch.darkink.docker_volume_watcher.trayapp.Windows;
using Prism.Logging;
using Prism.Mef;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ch.darkink.docker_volume_watcher.trayapp.Prism {
    public class Bootstrap : MefBootstrapper {

        /// <summary>
        /// Create the shell with WhellView
        /// </summary>
        /// <returns></returns>
        protected override DependencyObject CreateShell() {
            return null;
        }

        /// <summary>
        /// Show the shell
        /// </summary>
        //protected override void InitializeShell() {        }

        protected override void ConfigureModuleCatalog() {
            base.ConfigureModuleCatalog();
        }

        protected override void ConfigureContainer() {
            RegisterBootstrapperProvidedTypes();
        }

        protected override void RegisterBootstrapperProvidedTypes() {
            this.Container.ComposeExportedValue<ILoggerFacade>(this.Logger);
            this.Container.ComposeExportedValue<IModuleCatalog>(this.ModuleCatalog);
            this.Container.ComposeExportedValue<IServiceLocator>(new MefServiceLocatorAdapter(this.Container));
            this.Container.ComposeExportedValue<AggregateCatalog>(this.AggregateCatalog);
            this.Container.ComposeExportedValue<CompositionContainer>(this.Container);
        }

        protected override void ConfigureAggregateCatalog() {
            base.ConfigureAggregateCatalog();

            this.AggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(Bootstrap).Assembly));
        }

    }
}
