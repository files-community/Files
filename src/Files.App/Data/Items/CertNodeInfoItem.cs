// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public class CertNodeInfoItem
	{
        public string IssuedTo { get; set; } = string.Empty;

        public string IssuedBy { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string Thumbprint { get; set; } = string.Empty;

        public string ValidFrom { get; set; } = string.Empty;

        public string ValidTo { get; set; } = string.Empty;

        public string SignAlgorithm { get; set; } = string.Empty;
    }
}
