// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Details;

		public override string Label
			=> Strings.Details.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutDetailsDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Details");

		public override HotKey HotKey
			=> new(Keys.Number1, KeyModifiers.CtrlShift);
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutListAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.List;

		public override string Label
			=> Strings.List.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutListDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.List");

		public override HotKey HotKey
			=> new(Keys.Number2, KeyModifiers.CtrlShift);
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutCardsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Cards;

		public override string Label
			=> Strings.Cards.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutCardsDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Tiles");

		public override HotKey HotKey
			=> new(Keys.Number3, KeyModifiers.CtrlShift);
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutGridAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Grid;

		public override string Label
			=> Strings.Grid.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutGridDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconSize.Small");

		public override HotKey HotKey
			=> new(Keys.Number4, KeyModifiers.CtrlShift);
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutColumnsAction : ToggleLayoutAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		protected override LayoutTypes LayoutType
			=> LayoutTypes.Columns;

		public override string Label
			=> Strings.Columns.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutColumnsDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Columns");

		public override HotKey HotKey
			=> new(Keys.Number5, KeyModifiers.CtrlShift);

		public override bool IsExecutable
			=> ContentPageContext.PageType is not ContentPageTypes.RecycleBin;

		public LayoutColumnsAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Adaptive;

		public override string Label
			=> Strings.Adaptive.GetLocalizedResource();

		public override string Description
			=> Strings.LayoutAdaptiveDescription.GetLocalizedResource();

		public override bool IsExecutable
			=> Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Auto");

		public override HotKey HotKey
			=> new(Keys.Number6, KeyModifiers.CtrlShift);

		protected override void OnContextChanged(string propertyName)
		{
			if (propertyName is nameof(IDisplayPageContext.IsLayoutAdaptiveEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal abstract class ToggleLayoutAction : ObservableObject, IToggleAction
	{
		protected readonly IDisplayPageContext Context;

		protected abstract LayoutTypes LayoutType { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public virtual ActionCategory Category
			=> ActionCategory.Layout;

		public abstract RichGlyph Glyph { get; }

		public abstract HotKey HotKey { get; }

		public bool IsOn
			=> Context.LayoutType == LayoutType;

		public virtual bool IsExecutable
			=> true;

		public ToggleLayoutAction()
		{
			Context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			Context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			Context.LayoutType = LayoutType;

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.LayoutType))
				OnPropertyChanged(nameof(IsOn));

			if (e.PropertyName is not null)
				OnContextChanged(e.PropertyName);
		}

		protected virtual void OnContextChanged(string propertyName)
		{
		}
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutDecreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.DecreaseSize.GetLocalizedResource();

		public string Description
			=> Strings.LayoutDecreaseSizeDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public HotKey HotKey
			=> new(Keys.Subtract, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemMinus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings is not null;

		public LayoutDecreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var instanceViewModel = ContentPageContext.ShellPage?.InstanceViewModel;
			if (instanceViewModel?.FolderSettings is not { } folderSettings)
				return Task.CompletedTask;

			var settings = UserSettingsService.LayoutSettingsService;
			switch (folderSettings.LayoutMode)
			{
				case FolderLayoutModes.DetailsView when settings.DetailsViewSize > DetailsViewSizeKind.Compact:
					settings.DetailsViewSize -= 1;
					return Task.CompletedTask;
				case FolderLayoutModes.ListView when settings.ListViewSize > ListViewSizeKind.Compact:
					settings.ListViewSize -= 1;
					return Task.CompletedTask;
				case FolderLayoutModes.CardsView when settings.CardsViewSize > CardsViewSizeKind.Small:
					settings.CardsViewSize -= 1;
					return Task.CompletedTask;
				case FolderLayoutModes.GridView when settings.GridViewSize > GridViewSizeKind.Small:
					settings.GridViewSize -= 1;
					return Task.CompletedTask;
				case FolderLayoutModes.ColumnView when settings.ColumnsViewSize > ColumnsViewSizeKind.Compact:
					settings.ColumnsViewSize -= 1;
					return Task.CompletedTask;
			}

			LayoutCycler.Cycle(folderSettings, instanceViewModel.IsPageTypeRecycleBin, forward: false);
			return Task.CompletedTask;
		}
	}

	[GeneratedRichCommand]
	internal sealed partial class LayoutIncreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.IncreaseSize.GetLocalizedResource();

		public string Description
			=> Strings.LayoutIncreaseSizeDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Layout;

		public HotKey HotKey
			=> new(Keys.Add, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemPlus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings is not null;

		public LayoutIncreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var instanceViewModel = ContentPageContext.ShellPage?.InstanceViewModel;
			if (instanceViewModel?.FolderSettings is not { } folderSettings)
				return Task.CompletedTask;

			var settings = UserSettingsService.LayoutSettingsService;
			switch (folderSettings.LayoutMode)
			{
				case FolderLayoutModes.DetailsView when settings.DetailsViewSize < DetailsViewSizeKind.ExtraLarge:
					settings.DetailsViewSize += 1;
					return Task.CompletedTask;
				case FolderLayoutModes.ListView when settings.ListViewSize < ListViewSizeKind.ExtraLarge:
					settings.ListViewSize += 1;
					return Task.CompletedTask;
				case FolderLayoutModes.CardsView when settings.CardsViewSize < CardsViewSizeKind.ExtraLarge:
					settings.CardsViewSize += 1;
					return Task.CompletedTask;
				case FolderLayoutModes.GridView when settings.GridViewSize < GridViewSizeKind.ExtraLarge:
					settings.GridViewSize += 1;
					return Task.CompletedTask;
				case FolderLayoutModes.ColumnView when settings.ColumnsViewSize < ColumnsViewSizeKind.ExtraLarge:
					settings.ColumnsViewSize += 1;
					return Task.CompletedTask;
			}

			LayoutCycler.Cycle(folderSettings, instanceViewModel.IsPageTypeRecycleBin, forward: true);
			return Task.CompletedTask;
		}
	}

	internal static class LayoutCycler
	{
		private static readonly FolderLayoutModes[] Order =
		[
			FolderLayoutModes.DetailsView,
			FolderLayoutModes.ListView,
			FolderLayoutModes.CardsView,
			FolderLayoutModes.GridView,
			FolderLayoutModes.ColumnView,
		];

		public static void Cycle(LayoutPreferencesManager folderSettings, bool isRecycleBin, bool forward)
		{
			int currentIndex = Array.IndexOf(Order, folderSettings.LayoutMode);
			if (currentIndex < 0)
				return;

			int step = forward ? 1 : -1;
			int count = Order.Length;

			for (int i = 1; i <= count; i++)
			{
				var next = Order[((currentIndex + step * i) % count + count) % count];

				// Columns view is not supported inside the Recycle Bin.
				if (next is FolderLayoutModes.ColumnView && isRecycleBin)
					continue;

				// Reset the new layout's size so the next keystroke keeps growing / shrinking.
				var settings = Ioc.Default.GetRequiredService<IUserSettingsService>().LayoutSettingsService;
				switch (next)
				{
					case FolderLayoutModes.DetailsView:
						settings.DetailsViewSize = forward ? DetailsViewSizeKind.Compact : DetailsViewSizeKind.ExtraLarge;
						folderSettings.ToggleLayoutModeDetailsView(true);
						return;
					case FolderLayoutModes.ListView:
						settings.ListViewSize = forward ? ListViewSizeKind.Compact : ListViewSizeKind.ExtraLarge;
						folderSettings.ToggleLayoutModeList(true);
						return;
					case FolderLayoutModes.CardsView:
						settings.CardsViewSize = forward ? CardsViewSizeKind.Small : CardsViewSizeKind.ExtraLarge;
						folderSettings.ToggleLayoutModeCards(true);
						return;
					case FolderLayoutModes.GridView:
						settings.GridViewSize = forward ? GridViewSizeKind.Small : GridViewSizeKind.ExtraLarge;
						folderSettings.ToggleLayoutModeGridView(true);
						return;
					case FolderLayoutModes.ColumnView:
						settings.ColumnsViewSize = forward ? ColumnsViewSizeKind.Compact : ColumnsViewSizeKind.ExtraLarge;
						folderSettings.ToggleLayoutModeColumnView(true);
						return;
				}
			}
		}
	}
}
