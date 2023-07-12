using System.Threading;
using System.Threading.Tasks;

namespace Files.Sdk.Storage.NestedStorage
{
	/// <summary>
	/// Represents a storable resource that resides within a traversable folder structure.
	/// </summary>
	public interface INestedStorable : IStorable
	{
		/// <summary>
		/// Gets the containing folder for this item, if any.
		/// </summary>
		Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
