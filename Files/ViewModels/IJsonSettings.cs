using System.Collections.Generic;

namespace Files.ViewModels
{
    public interface IJsonSettings
    {
        Dictionary<string, List<string>> SavedBundles { get; set; }
    }
}
