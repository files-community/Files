using Files.App.Utils.Storage.Collection;

namespace Files.App.Extensions
{
    public static class CollectionExtensions
    {
		public static DisposableCollection<T> AsDisposableCollection<T>(this IList<T?> source) where T : IDisposable
		{
			return new DisposableCollection<T>(source);
		}
    }
}
