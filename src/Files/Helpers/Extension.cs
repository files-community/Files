using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

/// <summary>
/// Represents an extension in the ExtensionManager
/// </summary>
public class Extension : INotifyPropertyChanged
{
    #region Member Vars

    private PropertySet properties;
    private string serviceName;
    private readonly object sync = new object();

    public event PropertyChangedEventHandler PropertyChanged;

    public List<string> FileExtensions { get; internal set; } = new List<string>();

    #endregion Member Vars

    /// <summary>
    /// Creates an Extension object that represents an extension in the extension manager
    /// </summary>
    /// <param name="ext">The extension as represented by the system</param>
    /// <param name="properties">Properties about the extension</param>
    /// <param name="logo">The logo associated with the package that the extension is defined in</param>
    public Extension(AppExtension ext, PropertySet properties, BitmapImage logo)
    {
        AppExtension = ext;
        this.properties = properties;
        Enabled = false;
        Loaded = false;
        Offline = false;
        Logo = logo;
        Visible = Visibility.Collapsed;

        #region Properties

        serviceName = null;
        if (this.properties != null)
        {
            if (this.properties.ContainsKey("Service"))
            {
                PropertySet serviceProperty = this.properties["Service"] as PropertySet;
                serviceName = serviceProperty["#text"].ToString();
            }
        }

        #endregion Properties

        UniqueId = $"{ext.AppInfo.AppUserModelId}!{ext.Id}"; // The name that identifies this extension in the extension manager
    }

    #region Properties

    public BitmapImage Logo { get; private set; }

    /// <summary>
    /// Gets or sets the unique id of this extension which will be AppUserModel Id + Extension ID.
    /// </summary>
    public string UniqueId { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has enabled the extension or not.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package containing the extension is offline.
    /// </summary>
    public bool Offline { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the package has been loaded or not.
    /// </summary>
    public bool Loaded { get; private set; }

    public string PublicFolderPath { get; private set; }

    public AppExtension AppExtension { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether the extension should be visible in the list of extensions.
    /// </summary>
    public Visibility Visible { get; private set; }

    #endregion Properties

    /// <summary>
    /// Invoke the extension's app service
    /// </summary>
    /// <param name="message">The parameters for the app service call</param>
    public async Task<ValueSet> Invoke(ValueSet message)
    {
        if (Loaded)
        {
            try
            {
                // make the app service call
                using (var connection = new AppServiceConnection())
                {
                    // service name is defined in appxmanifest properties
                    connection.AppServiceName = serviceName;
                    // package Family Name is provided by the extension
                    connection.PackageFamilyName = AppExtension.Package.Id.FamilyName;

                    // open the app service connection
                    AppServiceConnectionStatus status = await connection.OpenAsync();
                    if (status != AppServiceConnectionStatus.Success)
                    {
                        Debug.WriteLine("Failed App Service Connection");
                    }
                    else
                    {
                        // Call the app service
                        AppServiceResponse response = await connection.SendMessageAsync(message);
                        if (response.Status == AppServiceResponseStatus.Success)
                        {
                            return response.Message;
                        }
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Calling the App Service failed");
            }
        }
        return new ValueSet(); // indicates an error from the app service
    }

    /// <summary>
    /// Called when an extension that has already been loaded is updated
    /// </summary>
    /// <param name="ext">The updated extension as represented by the system</param>
    /// <returns></returns>
    public async Task Update(AppExtension ext)
    {
        // ensure this is the same uid
        string identifier = ext.AppInfo.AppUserModelId + "!" + ext.Id;
        if (identifier != this.UniqueId)
        {
            return;
        }

        var properties = await ext.GetExtensionPropertiesAsync() as PropertySet;

        // get the logo for the extension
        var filestream = await (ext.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(1, 1))).OpenReadAsync();
        BitmapImage logo = new BitmapImage();
        logo.SetSource(filestream);

        // update the extension
        this.AppExtension = ext;
        this.properties = properties;
        Logo = logo;

        #region Update Properties

        // update app service information
        serviceName = null;
        if (this.properties != null)
        {
            if (this.properties.ContainsKey("Service"))
            {
                PropertySet serviceProperty = this.properties["Service"] as PropertySet;
                this.serviceName = serviceProperty["#text"].ToString();
            }
        }

        #endregion Update Properties

        await MarkAsLoaded();
    }

    /// <summary>
    /// Prepares the extension so that the ExtensionManager can present it as an available extension
    /// </summary>
    /// <returns></returns>
    public async Task MarkAsLoaded()
    {
        // make sure package is OK to load
        if (!AppExtension.Package.Status.VerifyIsOK())
        {
            return;
        }

        Enabled = true;

        // Don't reload
        if (Loaded)
        {
            return;
        }

        // The public folder is shared between the extension and the host.
        // We don't use it in this sample but you can see https://github.com/Microsoft/Build2016-B808-AppExtensibilitySample for an example of it can be used.
        StorageFolder folder = await AppExtension.GetPublicFolderAsync();
        PublicFolderPath = folder.Path;
        try
        {
            var file = await folder.GetFileAsync("FileExtensions.json");
            var text = await FileIO.ReadTextAsync(file);
            FileExtensions = JsonConvert.DeserializeObject<List<string>>(text);
        }
        catch
        {
            Debug.WriteLine("Unable to get extensions");
        }

        Loaded = true;
        Visible = Visibility.Visible;
        RaisePropertyChanged(nameof(Visible));
        Offline = false;
    }

    /// <summary>
    /// Enable the extension for use
    /// </summary>
    /// <returns></returns>
    public async Task Enable()
    {
        Enabled = true;
        await MarkAsLoaded();
    }

    /// <summary>
    /// Indicates to the extension manager that the extension is unloaded
    /// </summary>
    public void Unload()
    {
        // unload it
        lock (sync) // Calls to this functioned are queued on an await call so lock to handle one at a time
        {
            if (Loaded)
            {
                // see if the package is offline
                if (!AppExtension.Package.Status.VerifyIsOK() && !AppExtension.Package.Status.PackageOffline)
                {
                    Offline = true;
                }

                Loaded = false;
                Visible = Visibility.Collapsed;
                RaisePropertyChanged(nameof(Visible));
            }
        }
    }

    // user-facing action to disable the extension
    public void Disable()
    {
        // only disable if it is enabled so that we don't Unload() more than once
        if (Enabled)
        {
            Enabled = false;
            Unload();
        }
    }

    #region PropertyChanged

    /// <summary>
    /// Typical property changed handler so that the UI will update
    /// </summary>
    /// <param name="name"></param>
    private void RaisePropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion PropertyChanged
}