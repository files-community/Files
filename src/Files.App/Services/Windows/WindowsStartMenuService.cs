using System.IO;
using Windows.Storage;
using Windows.UI.StartScreen;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStartMenuService"/>
	internal sealed class StartMenuService : IStartMenuService
	{
		[Obsolete("See IStartMenuService for further information.")]
		public bool IsPinned(string itemPath)
		{
			var tileId = GetNativeTileId(itemPath);
			return SecondaryTile.Exists(tileId);
		}

		/// <inheritdoc/>
		public Task<bool> IsPinnedAsync(IStorable storable)
		{
			var tileId = GetNativeTileId(storable.Id);
			var exists = SecondaryTile.Exists(tileId);

			return Task.FromResult(exists);
		}

		/// <inheritdoc/>
		public async Task PinAsync(IStorable storable, string? displayName = null)
		{
			var tileId = GetNativeTileId(storable.Id);
			displayName ??= storable.Name;

			try
			{
				var path150x150 = ExtractFileIcon(storable, tileId);
				var path71x71 = path150x150;
				var path44x44 = path150x150;
				var path30x30 = path150x150;
				var path310x150 = path150x150;

				var tile = new SecondaryTile(
					tileId,
					displayName,
					storable.Id,
					path150x150,
					TileSize.Default)
				{
					VisualElements =
					{
						Square71x71Logo = path71x71,
						Square150x150Logo = path150x150,
						Square44x44Logo = path44x44,
						Square30x30Logo = path30x30,
						Wide310x150Logo = path310x150,
						ShowNameOnSquare150x150Logo = true,
						//BackgroundColor = Microsoft.UI.Colors.
					}
				};

				WinRT.Interop.InitializeWithWindow.Initialize(tile, MainWindow.Instance.WindowHandle);

				await tile.RequestCreateAsync();
			}
			catch (Exception e)
			{
				Debug.WriteLine(tileId);
				Debug.WriteLine(e.ToString());
			}

		}

		/// <inheritdoc/>
		public async Task UnpinAsync(IStorable storable)
		{
			var startScreen = StartScreenManager.GetDefault();
			var tileId = GetNativeTileId(storable.Id);

			await startScreen.TryRemoveSecondaryTileAsync(tileId);

			try
			{
				var iconFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("startmenu", CreationCollisionOption.OpenIfExists);
				var iconFile = await iconFolder.GetFileAsync($"{tileId}.png");
				await iconFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
			catch (FileNotFoundException) { }
		}

		private static string GetNativeTileId(string id)
		{
			// Remove symbols because windows doesn't like them in the ID, and will blow up
			var str = $"folder-{new string(id.Where(c => char.IsLetterOrDigit(c)).ToArray())}";

			// If the id string is too long, Windows will throw an error, so remove every other character
			if (str.Length > 64)
				str = new string(str.Where((_, i) => i % 2 == 0).ToArray());

			return str;
		}

		private static unsafe Uri ExtractFileIcon(IStorable file, string id)
		{
			var fileName = $"{id}.png";
			var iconFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "startmenu");
			var iconPath = Path.Combine(iconFolder, fileName);
			Directory.CreateDirectory(iconFolder);

			HICON hIcon = default;
			uint piconid = 0;
			uint extractedCount = 0;

			extractedCount = PInvoke.PrivateExtractIcons(
				file.Id,
				0,
				256,
				256,
				new Span<HICON>(&hIcon, 1),
				out piconid,
				0
			);

			using (var managedIcon = System.Drawing.Icon.FromHandle((nint)hIcon.Value))
			using (var bitmap = managedIcon.ToBitmap())
			{
				bitmap.Save(iconPath, System.Drawing.Imaging.ImageFormat.Png);
			}

			var destroyResult = PInvoke.DestroyIcon(hIcon);

			return new Uri($"ms-appdata:///local/startmenu/{fileName}");
		}
	}
}
