using Files.Shared.Utils;

namespace Files.App.Data.Items
{
	[Bindable(true)]
	public sealed partial class ShelfItem : ObservableObject, IWrapper<IStorable>, IAsyncInitialize
	{
		private readonly IImageService _imageService;

		[ObservableProperty] private IImage? _Icon;
		[ObservableProperty] private string? _Name;
		[ObservableProperty] private string? _Path;

		/// <inheritdoc/>
		public IStorable Inner { get; }

		public ShelfItem(IStorable storable, IImage? icon = null)
		{
			_imageService = Ioc.Default.GetRequiredService<IImageService>();
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
	}
}
