using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    internal class ExtensionManager
    {
        //private CoreDispatcher _dispatcher; // used to run code on the UI thread for code that may update UI
        private AppExtensionCatalog catalog; // the catalog of app extensions available to this host

        /// <summary>
        /// Builds a collection of extensions available to this host
        /// </summary>
        /// <param name="extensionContractName">
        /// The contract string is defined in the extension host's Package.appxmanifest file under <uap3:AppExtensionHost><uap3:Name>MathExt</uap3:Name></uap3:AppExtensionHost>
        /// The string is defined in the extension's Package.appxmanifest file under <uap3:Extension Category="windows.appExtension"><uap3:AppExtension Name = "MathExt" ... >
        /// When these two strings match, the extension is loaded.</param>
        public ExtensionManager(string extensionContractName)
        {
            // catalog & contract
            ExtensionContractName = extensionContractName;
            catalog = AppExtensionCatalog.Open(ExtensionContractName);
            //_dispatcher = null;
        }

        // The collection of extensions for this host
        public ObservableCollection<Extension> Extensions { get; } = new ObservableCollection<Extension>();

        // new jtw
        public Extension GetExtension(string id)
        {
            return Extensions.Where(e => e.UniqueId == id).FirstOrDefault();
        }

        // The name that extensions must specify to be considered an extension this host can load
        public string ExtensionContractName { get; private set; }

        /// <summary>
        /// Sets up handlers for package events
        /// Be sure to call this from the UI thread.
        /// </summary>
        /// <param name="dispatcher"></param>
        public void Initialize()
        {
            #region Error Checking & Dispatcher Setup

            // verify that we haven't already been initialized
            //if (_dispatcher != null)
            //{
            //    throw new ExtensionManagerException("Extension Manager for " + this.ExtensionContractName + " is already initialized.");
            //}

            //_dispatcher = dispatcher;

            #endregion Error Checking & Dispatcher Setup

            // handlers for extension management events
            //_catalog.PackageInstalled += Catalog_PackageInstalled;
            //_catalog.PackageUpdated += Catalog_PackageUpdated;
            //_catalog.PackageUninstalling += Catalog_PackageUninstalling;
            //_catalog.PackageUpdating += Catalog_PackageUpdating;
            //_catalog.PackageStatusChanged += Catalog_PackageStatusChanged; // raised when a package changes with respect to integrity, licensing state, or availability (package is installed on a SD card that is then unplugged; you wouldn't get an uninstalling event)

            FindAndLoadExtensions();
        }

        /// <summary>
        /// Find all of the extensions currently installed on the machine that have the same name specified by this host
        /// in its package.appxmanifest file and load them
        /// </summary>
        public async void FindAndLoadExtensions()
        {
            #region Error Checking

            // Run on the UI thread because the Extensions Tab UI updates as extensions are added or removed
            //if (_dispatcher == null)
            //{
            //    throw new ExtensionManagerException("Extension Manager for " + this.ExtensionContractName + " is not initialized.");
            //}

            #endregion Error Checking

            IReadOnlyList<AppExtension> extensions = await catalog.FindAllAsync();
            foreach (AppExtension ext in extensions)
            {
                await LoadExtension(ext);
            }
        }

        /// <summary>
        /// Handles a new package installed event. The new package may contain extensions this host can use.
        /// </summary>
        /// <param name="sender">The catalog that the extensions belong to</param>
        /// <param name="args">Contains the package that was installed</param>
        private async void Catalog_PackageInstalled(AppExtensionCatalog sender, AppExtensionPackageInstalledEventArgs args)
        {
            foreach (AppExtension ext in args.Extensions)
            {
                await LoadExtension(ext);
            }
        }

        /// <summary>
        /// A package has been updated. Reload its extensions.
        /// </summary>
        /// <param name="sender">The catalog that the extensions belong to</param>
        /// <param name="args">Contains the package that was updated</param>
        private async void Catalog_PackageUpdated(AppExtensionCatalog sender, AppExtensionPackageUpdatedEventArgs args)
        {
            foreach (AppExtension ext in args.Extensions)
            {
                await LoadExtension(ext);
            }
        }

        /// <summary>
        /// A package is being updated. Unload all the extensions in the package.
        /// </summary>
        /// <param name="sender">The catalog that the extensions belong to</param>
        /// <param name="args">Contains the package that is updating</param>
        private void Catalog_PackageUpdating(AppExtensionCatalog sender, AppExtensionPackageUpdatingEventArgs args)
        {
            UnloadExtensions(args.Package);
        }

        /// <summary>
        /// A package has been removed. Remove all the extensions in the package.
        /// </summary>
        /// <param name="sender">The catalog that the extensions belong to</param>
        /// <param name="args">Contains the package that is uninstalling</param>
        private void Catalog_PackageUninstalling(AppExtensionCatalog sender, AppExtensionPackageUninstallingEventArgs args)
        {
            RemoveExtensions(args.Package);
        }

        /// <summary>
        /// The status of a package has changed (it could be a licensing issue, the package was on USB and has been removed removed, etc)
        /// Unload extensions if a package went offline or is otherwise no longer available
        /// </summary>
        /// <param name="sender">The catalog that the extensions belong to</param>
        /// <param name="args">Contains the package that has changed status</param>
        private void Catalog_PackageStatusChanged(AppExtensionCatalog sender, AppExtensionPackageStatusChangedEventArgs args)
        {
            if (!args.Package.Status.VerifyIsOK()) // If the package isn't ok, unload its extensions
            {
                // if it's offline, unload its extensions
                if (args.Package.Status.PackageOffline)
                {
                    UnloadExtensions(args.Package);
                }
                else if (args.Package.Status.Servicing || args.Package.Status.DeploymentInProgress)
                {
                    // if the package is being serviced or deployed, ignore the status events
                }
                else
                {
                    // Deal with an invalid or tampered with package, or other issue, by removing the extensions
                    // Adding a UI glyph to the affected extensions could be a good user experience if you wish
                    RemoveExtensions(args.Package);
                }
            }
            else // The package is now OK--attempt to load its extensions
            {
                LoadExtensions(args.Package);
            }
        }

        /// <summary>
        /// Load an extension
        /// </summary>
        /// <param name="ext">Represents the extension to load</param>
        /// <returns></returns>
        public async Task LoadExtension(AppExtension ext)
        {
            // Build a unique identifier for this extension
            string identifier = $"{ext.AppInfo.AppUserModelId}!{ext.Id}";

            // load the extension if the package is OK
            if (!ext.Package.Status.VerifyIsOK()
                /* This is a good place to do package signature verification
                   For the purpose of the sample, we ignore where the package is from
                   Here is an example of how you would ensure that you only load store-signed extensions:
                    && ext.Package.SignatureKind == PackageSignatureKind.Store */
                )
            {
                return; // Because this package doesn't meet our requirements, don't load it
            }

            // if we already have an extension by this name then then this load is really an update to the extension
            Extension existingExt = Extensions.Where(e => e.UniqueId == identifier).FirstOrDefault();

            // New extension?
            if (existingExt == null)
            {
                // get the extension's properties, such as its logo
                var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;
                var filestream = await ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1)).OpenReadAsync();
                BitmapImage logo = new BitmapImage();
                logo.SetSource(filestream);

                Extension newExtension = new Extension(ext, properties, logo);
                Extensions.Add(newExtension);

                await newExtension.MarkAsLoaded();
            }
            else // update scenario
            {
                // unload the old version of the extension first
                existingExt.Unload();

                // update the extension
                await existingExt.Update(ext);
            }
        }

        /// <summary>
        /// Loads all extensions associated with a package - used for when package status comes back
        /// </summary>
        /// <param name="package">Package containing the extensions to load</param>
        /// <returns></returns>
        public void LoadExtensions(Package package)
        {
            Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(async e => { await e.MarkAsLoaded(); });
        }

        /// <summary>
        /// Unloads all extensions associated with a package - used for updating and when package status goes away
        /// </summary>
        /// <param name="package">Package containing the extensions to unload</param>
        /// <returns></returns>
        public void UnloadExtensions(Package package)
        {
            Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.Unload(); });
            //// Run on the UI thread because the Extensions Tab UI updates as extensions are added or removed
            //await _dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
            //});
        }

        /// <summary>
        /// Removes all extensions associated with a package
        /// Useful when removing a package or a package becomes invalid
        /// </summary>
        /// <param name="package">The package containing the extensions to remove</param>
        /// <returns></returns>
        public void RemoveExtensions(Package package)
        {
            Extensions.Where(ext => ext.AppExtension.Package.Id.FamilyName == package.Id.FamilyName).ToList().ForEach(e => { e.Unload(); Extensions.Remove(e); });
        }

        /// <summary>
        /// Asks the user whether to uninstall the package containing an extension from the machine
        /// and removes it if the user agrees.
        /// </summary>
        /// <param name="ext"></param>
        public async void RemoveExtension(Extension ext)
        {
            await catalog.RequestRemovePackageAsync(ext.AppExtension.Package.Id.FullName);
        }

        #region Extra exceptions

        // For exceptions using the Extension Manager
        public class ExtensionManagerException : Exception
        {
            public ExtensionManagerException()
            {
            }

            public ExtensionManagerException(string message) : base(message)
            {
            }

            public ExtensionManagerException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        #endregion Extra exceptions
    }
}