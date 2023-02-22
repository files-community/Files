using Files.Sdk.Storage.LocatableStorage;

namespace Files.Core.AppModels
{
    /// <summary>
	/// Represents an item that is tagged.
	/// </summary>
	/// <param name="TagUids">Tag UIDs that the item is tagged with.</param>
	/// <param name="Storable">The item that contains the tags.</param>
    public sealed record class TaggedItemModel(string[] TagUids, ILocatableStorable Storable);
}
