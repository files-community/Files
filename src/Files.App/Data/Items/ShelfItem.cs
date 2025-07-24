// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using System.Windows.Documents;

namespace Files.App.Data.Items
{
	[Bindable(true)]
	public sealed partial class ShelfItem : ObservableObject, IWrapper<IStorableChild>, IAsyncInitialize
	{
		private readonly IImageService _imageService;
		private readonly ICollection<ShelfItem> _sourceCollection;

		[ObservableProperty] public partial IImage? Icon { get; set; }
		[ObservableProperty] public partial string? Name { get; set; }
		[ObservableProperty] public partial string? Path { get; set; }

		/// <inheritdoc/>
		public IStorableChild Inner { get; }

		public ShelfItem(IStorableChild storable, ICollection<ShelfItem> sourceCollection, IImage? icon = null)
		{
			_imageService = Ioc.Default.GetRequiredService<IImageService>();
			_sourceCollection = sourceCollection;
			Inner = storable;
			Icon = icon;
			Name = storable.Name;
			Path = storable.Id;
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			Icon = await _imageService.GetIconAsync(Inner, cancellationToken);
		}

		[RelayCommand]
		public async Task ViewInFolderAsync(CancellationToken cancellationToken)
		{
			var context = Ioc.Default.GetRequiredService<IContentPageContext>();
			if (context.ShellPage is not { } shellPage)
				return;

			var parent = await Inner.GetParentAsync(cancellationToken);
			if (parent is null)
				return;

			await NavigationHelpers.OpenPath(parent.Id, shellPage);
		}

		[RelayCommand]
		public void Remove()
		{
			_sourceCollection.Remove(this);
		}
	}
}
