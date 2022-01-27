using System;

namespace Files.Backend.Models.Icons
{
    public sealed class UriIconModel : IconModel
    {
        public Uri UriSource { get; set; }

        // TODO(i): Add image type, or leave as is and use base.AdditionalFormat
    }
}
