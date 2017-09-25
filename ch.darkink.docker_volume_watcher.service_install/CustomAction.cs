using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

namespace ch.darkink.docker_volume_watcher.service_install {

    public class CustomActions {

        internal class Win32Helper {

            #region Constants

            internal const Int32 GENERIC_READ = 0x2008D; //unchecked((int)0x80000000);
            internal const Int32 SERVICE_START = 0x0010;
            internal const Int32 SERVICE_STOP = 0x0020;

            internal const Int32 ERROR_INSUFFICIENT_BUFFER = 122;
            internal const Int32 SECURITY_MAX_SID_SIZE = 68;

            #endregion

            #region Structures

            #endregion

            #region Methods

            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern Boolean QueryServiceObjectSecurity(SafeHandle serviceHandle, System.Security.AccessControl.SecurityInfos secInfo, Byte[] lpSecDesrBuf, UInt32 bufSize, out UInt32 bufSizeNeeded);

            [DllImport("advapi32.dll", SetLastError = true)]
            internal static extern Boolean SetServiceObjectSecurity(SafeHandle serviceHandle, System.Security.AccessControl.SecurityInfos secInfos, Byte[] lpSecDesrBuf);

            #endregion
            
        }

        /// <summary>
        /// Executes the on after install.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        [CustomAction]
        public static ActionResult ExecuteOnAfterInstall(Session session) {
            session.Log("Begin ExecuteOnAfterInstall");
            String serviceName = session.CustomActionData["SERVICE_NAME"];
            String eventLogSource = session.CustomActionData["EVENT_LOG_SOURCE"];
            String eventLogName = session.CustomActionData["EVENT_LOG_NAME"];

            try {
                ServiceController sc = new ServiceController(serviceName);
                SetUserAccessServiceDACL(sc.ServiceHandle);

                if (!EventLog.SourceExists(eventLogSource)) {
                    EventLog.CreateEventSource(eventLogSource, eventLogName);
                }
            } catch (Exception ex) {
                session.Log("Error during ExecuteOnAfterInstall : " + ex.Message);
                return ActionResult.Failure;
            }

            session.Log("ExecuteOnAfterInstall Finished");
            return ActionResult.Success;
        }

        /// <summary>
        /// Executes the on after uninstall.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult ExecuteOnAfterUninstall(Session session) {
            session.Log("Begin ExecuteOnAfterUninstall");
            String eventLogSource = session.CustomActionData["EVENT_LOG_SOURCE"];
            String eventLogName = session.CustomActionData["EVENT_LOG_NAME"];

            try {
                if (EventLog.SourceExists(eventLogSource)) {
                    EventLog.DeleteEventSource(eventLogSource);
                    EventLog.Delete(eventLogName);
                }
            } catch { }
            session.Log("ExecuteOnAfterUninstall Finished");
            return ActionResult.Success;
        }

        private static void SetUserAccessServiceDACL(SafeHandle service) {
            Int32 err = 0;

            //Get the security descriptor
            Byte[] psd = new Byte[0];
            if (!Win32Helper.QueryServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, psd, 0, out UInt32 neeeded)) {
                err = Marshal.GetLastWin32Error();
                if (err != Win32Helper.ERROR_INSUFFICIENT_BUFFER) {
                    throw new InvalidOperationException("Could not query service object security size : " + err);
                }

                UInt32 size = neeeded;
                psd = new Byte[size];
                if (!Win32Helper.QueryServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, psd, size, out neeeded)) {
                    throw new InvalidOperationException("Could not allocate security descriptor : " + Marshal.GetLastWin32Error());
                }
            }

            RawSecurityDescriptor rsd = new RawSecurityDescriptor(psd, 0);
            RawAcl racl = rsd.DiscretionaryAcl;
            DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, racl);

            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            dacl.AddAccess(AccessControlType.Allow, sid, (Int32)(Win32Helper.SERVICE_START | Win32Helper.SERVICE_STOP | Win32Helper.GENERIC_READ), InheritanceFlags.None, PropagationFlags.None);

            Byte[] rawdacl = new Byte[dacl.BinaryLength];
            dacl.GetBinaryForm(rawdacl, 0);
            rsd.DiscretionaryAcl = new RawAcl(rawdacl, 0);

            Byte[] rawsd = new Byte[rsd.BinaryLength];
            rsd.GetBinaryForm(rawsd, 0);

            if (!Win32Helper.SetServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, rawsd)) {
                throw new InvalidOperationException("Could not set object security : " + Marshal.GetLastWin32Error());
            }

            Console.WriteLine("User access was set successfully on the service.");
        }


    }
}
