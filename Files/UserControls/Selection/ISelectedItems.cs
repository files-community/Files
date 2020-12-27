namespace Files.UserControls.Selection
{
    public interface ISelectedItems
    {
        void Add(object item);

        void Clear();

        bool Contains(object item);

        void Remove(object item);
    }
}