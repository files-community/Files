using Files.Enums;

namespace Files.Filesystem.Cloud
{
    public class CloudProvider
    {
        public CloudProviders ID { get; set; }

        public string Name { get; set; }

        public string SyncFolder { get; set; }
    }
}