using System;
using System.Collections.Generic;
using System.Text;

namespace Files.Common
{
    public enum KnownCloudProviders
    {
        ONEDRIVE,
        ONEDRIVE_BUSINESS,
        MEGASYNC,
        GOOGLEDRIVE,
        DROPBOX
    }

    public class CloudProvider
    {
        public KnownCloudProviders ID { get; set; }
        public string SyncFolder { get; set; }
        public string Name { get; set; }
    }
}
