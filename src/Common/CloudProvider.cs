using System;

namespace Files.Common
{
    public class CloudProvider : IEquatable<CloudProvider>
    {
        public CloudProviders ID { get; set; }

        public string Name { get; set; }

        public string SyncFolder { get; set; }

        public override int GetHashCode()
        {
            return $"{ID}|{SyncFolder}".GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CloudProvider other)
            {
                return Equals(other);
            }
            return base.Equals(obj);
        }

        public bool Equals(CloudProvider other)
        {
            return other != null && other.ID == ID && other.SyncFolder == SyncFolder;
        }
    }

    public enum CloudProviders
    {
        OneDrive,
        OneDriveCommercial,
        Mega,
        GoogleDrive,
        DropBox,
        AppleCloud,
        AmazonDrive,
        Box
    }
}