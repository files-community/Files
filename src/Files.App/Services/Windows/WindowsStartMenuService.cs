using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
				var fileIcon = ExtractFileIcon(storable, tileId);

				var tile = new SecondaryTile(
					tileId,
					displayName,
					$"files-dev.exe \"{storable.Id}\"",
					fileIcon,
					TileSize.Default)
				{
					VisualElements =
					{
						Square44x44Logo = fileIcon,
						ShowNameOnSquare150x150Logo = true
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
			return new Guid(SHA1.HashData(Encoding.UTF8.GetBytes(id.ToLowerInvariant()))[..16]).ToString();
		}

		private static Uri ExtractFileIcon(IStorable file, string id)
		{
			var fileName = $"{id}.png";
			var iconFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "startmenu");
			var iconPath = Path.Combine(iconFolder, fileName);
			Directory.CreateDirectory(iconFolder);

			try
			{
				using (var managedIcon = Icon.ExtractAssociatedIcon(file.Id))
				{
					using (var bitmap = managedIcon!.ToBitmap())
					{
						bitmap.Save(iconPath, ImageFormat.Png);
					}
				}

				return new Uri($"ms-appdata:///local/startmenu/{fileName}");
			}
			catch
			{
				int shell32IconId = file is IFolder ? 4 : 0;

				using (var managedIcon = Icon.ExtractIcon(Path.Combine(Environment.SystemDirectory, "shell32.dll"), shell32IconId))
				{
					using (var bitmap = managedIcon!.ToBitmap())
					{
						bitmap.Save(iconPath, ImageFormat.Png);
					}
				}

				return new Uri($"ms-appdata:///local/startmenu/{fileName}");
			}
		}
	}
}
