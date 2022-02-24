namespace Files.Filesystem.Search
{
    public interface ISearchTag
    {
        ISearchFilter Filter { get; }

        string Title { get; }
        string Parameter { get; }

        void Delete();
    }
}
