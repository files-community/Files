using Files.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Files.Filesystem.Search
{
    public interface ISearchHeader
    {
        SearchKeys Key { get; }

        string Glyph { get; }
        string Label { get; }
        string Description { get; }

        ISearchFilter CreateFilter();
    }

    public interface ISearchHeaderProvider
    {
        ISearchHeader GetHeader(SearchKeys key);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SearchHeaderAttribute : Attribute
    {
        public SearchKeys Key { get; set; } = SearchKeys.None;

        public SearchHeaderAttribute() {}
        public SearchHeaderAttribute(SearchKeys key) => Key = key;
    }

    internal class SearchHeaderProvider : ISearchHeaderProvider
    {
        private readonly IReadOnlyDictionary<SearchKeys, ISearchHeader> headers
            = new ReadOnlyDictionary<SearchKeys, ISearchHeader>(GetHeaders().ToDictionary(header => header.Key));

        public ISearchHeader GetHeader(SearchKeys key) => headers?[key];

        private static IEnumerable<ISearchHeader> GetHeaders()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(SearchHeaderAttribute), false).Cast<SearchHeaderAttribute>();
                foreach (var attribute in attributes)
                {
                    yield return attribute.Key is SearchKeys.None
                        ? Activator.CreateInstance(type) as ISearchHeader
                        : Activator.CreateInstance(type, new object[] { attribute.Key }) as ISearchHeader;
                }
            }
        }
    }
}
