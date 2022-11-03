using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Files.App.UserControls
{
	public sealed partial class PaneControl : UserControl, IPane
	{
		private PaneContents content;

		private IPaneSettingsService PaneSettingsService { get; } = Ioc.Default.GetService<IPaneSettingsService>();

		public PanePositions Position => Panel.Content is IPane pane ? pane.Position : PanePositions.Right;

		public PaneControl()
		{
			InitializeComponent();

			PaneSettingsService.PropertyChanged += PaneService_PropertyChanged;
			Update();
		}

		public void UpdatePosition(double panelWidth, double panelHeight)
		{
			if (Panel.Content is IPane pane)
			{
				pane.UpdatePosition(panelWidth, panelHeight);
			}
			if (Panel.Content is Control control)
			{
				MinWidth = control.MinWidth;
				MaxWidth = control.MaxWidth;
				MinHeight = control.MinHeight;
				MaxHeight = control.MaxHeight;
			}
		}

		private void PaneService_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IPaneSettingsService.Content))
			{
				Update();
			}
		}

		private void Update()
		{
			var newContent = PaneSettingsService.Content;
			if (content != newContent)
			{
				content = newContent;
				Panel.Content = GetPane(content);
			}
		}

		private static Control GetPane(PaneContents content) => content switch
		{
			PaneContents.Preview => new PreviewPane(),
			_ => null,
		};
	}
}
