using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Files.App.Commands;
using Files.App.Contexts;
using Files.Backend.Helpers;

namespace Files.App.Actions;

internal class InstallCertificateAction : ObservableObject, IAction
{
	private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

	public string Label => "Install".GetLocalizedResource();

	public string Description => "InstallCertificateDescription".GetLocalizedResource();

	public RichGlyph Glyph { get; } = new("\uEB95");
	
	public bool IsExecutable => context.SelectedItems.Count == 1 &&
	                            context.SelectedItems.All(x => FileExtensionHelpers.IsCertificateFile(x.FileExtension)) &&
	                            context.PageType is not ContentPageTypes.RecycleBin and not ContentPageTypes.ZipFolder;

	public InstallCertificateAction()
	{
		context.PropertyChanged += Context_PropertyChanged;
	}

	public Task ExecuteAsync()
	{
		foreach (ListedItem selectedItem in context.SelectedItems)
		{
			try
			{
				X509Certificate2 certificate = new X509Certificate2(selectedItem.ItemPath);
				X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

				store.Open(OpenFlags.ReadWrite);
				try
				{
					store.Add(certificate);
				}
				finally
				{
					store.Close();
				}
			}
			catch (CryptographicException)
			{
				break;
			}
		}

		return Task.CompletedTask;
	}

	private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IContentPageContext.SelectedItems))
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}