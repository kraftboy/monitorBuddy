using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Windows.Management.Deployment;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace monitorBuddyTray
{
    public class StartUp
    {
        [STAThread]
        public static bool RunStartup()
        {
            //if app isn't running with identity, register its sparse package
            if (!ExecutionMode.IsRunningWithIdentity())
            {
                //TODO - update the value of externalLocation to match the output location of your VS Build binaries and the value of 
                //sparsePkgPath to match the path to your signed Sparse Package (.msix). 
                //Note that these values cannot be relative paths and must be complete paths
                string externalLocation = AppDomain.CurrentDomain.BaseDirectory;
                string sparsePkgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msix", "out", "monitorBuddyTray.msix");

                //Attempt registration
                if (registerSparsePackage(externalLocation, sparsePkgPath))
                {
                    //Registration succeded, restart the app to run with identity
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location, arguments: "");

                }
                else //Registration failed, run without identity
                {
                    return true;
                }

            }

            DesktopNotificationManagerCompat.RegisterActivator<ToastActivator>();

            //App is registered and running with identity, handle launch and activation
            return true;
        }


        private static bool registerSparsePackage(string externalLocation, string sparsePkgPath)
        {
            bool registration = false;
            try
            {
                Uri externalUri = new Uri(externalLocation);
                Uri packageUri = new Uri(sparsePkgPath);

                Console.WriteLine("exe Location {0}", externalLocation);
                Console.WriteLine("msix Address {0}", sparsePkgPath);

                Console.WriteLine("  exe Uri {0}", externalUri);
                Console.WriteLine("  msix Uri {0}", packageUri);

                PackageManager packageManager = new PackageManager();

                //Declare use of an external location
                var options = new AddPackageOptions
                {
                    ExternalLocationUri = externalUri
                };

                Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = packageManager.AddPackageByUriAsync(packageUri, options);

                ManualResetEvent opCompletedEvent = new ManualResetEvent(false); // this event will be signaled when the deployment operation has completed.

                deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

                Console.WriteLine("Installing package {0}", sparsePkgPath);

                Debug.WriteLine("Waiting for package registration to complete...");

                opCompletedEvent.WaitOne();

                if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Error)
                {
                    Windows.Management.Deployment.DeploymentResult deploymentResult = deploymentOperation.GetResults();
                    Debug.WriteLine("Installation Error: {0}", deploymentOperation.ErrorCode);
                    Debug.WriteLine("Detailed Error Text: {0}", deploymentResult.ErrorText);

                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Canceled)
                {
                    Debug.WriteLine("Package Registration Canceled");
                }
                else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Completed)
                {
                    registration = true;
                    Debug.WriteLine("Package Registration succeeded!");
                }
                else
                {
                    Debug.WriteLine("Installation status unknown");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AddPackageSample failed, error message: {0}", ex.Message);
                Console.WriteLine("Full Stacktrace: {0}", ex.ToString());

                return registration;
            }

            return registration;
        }

        private static void removeSparsePackage() //example of how to uninstall a Sparse Package
        {
            PackageManager packageManager = new PackageManager();
            Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = packageManager.RemovePackageAsync("PhotoStoreDemo_0.0.0.1_x86__rg009sv5qtcca");
            ManualResetEvent opCompletedEvent = new ManualResetEvent(false); // this event will be signaled when the deployment operation has completed.

            deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

            Debug.WriteLine("Uninstalling package..");
            opCompletedEvent.WaitOne();
        }

    }
}