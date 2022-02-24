#nullable enable

using System.Collections.Generic;

namespace Files.Backend.Models
{
    public interface ICustomFormattable
    {
        IReadOnlyCollection<string>? Formats { get;}

        void AppendFormat(string formatInfo);
    }
}
