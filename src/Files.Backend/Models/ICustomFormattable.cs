using System.Collections.Generic;

namespace Files.Backend.Models
{
    public interface ICustomFormattable
    {
        IReadOnlyCollection<string>? Formats { get; }

        bool AppendFormat(string formatInfo);
    }
}
