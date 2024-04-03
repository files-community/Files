// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Details;

		public override string Label
			=> "Details".GetLocalizedResource();

		public override string Description
			=> "LayoutDetailsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "Icons.DetailsLayout.16x16");

		public override HotKey HotKey
			=> new(Keys.Number1, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutListAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.List;

		public override string Label
			=> "List".GetLocalizedResource();

		public override string Description
			=> "LayoutListDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "Icons.ListLayout.16x16");

		public override HotKey HotKey
			=> new(Keys.Number2, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Tiles;

		public override string Label
			=> "Tiles".GetLocalizedResource();

		public override string Description
			=> "LayoutTilesDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "Icons.TilesLayout.16x16");

		public override HotKey HotKey
			=> new(Keys.Number3, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutGridAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Grid;

		public override string Label
			=> "Grid".GetLocalizedResource();

		public override string Description
			=> "LayoutGridDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "Icons.GridLayout.16x16");

		public override HotKey HotKey
			=> new(Keys.Number4, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutColumnsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Columns;

		public override string Label
			=> "Columns".GetLocalizedResource();

		public override string Description
			=> "LayoutColumnsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "Icons.ColumnsLayout.16x16");

		public override HotKey HotKey
			=> new(Keys.Number5, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Adaptive;

		public override string Label
			=> "Adaptive".GetLocalizedResource();

		public override string Description
			=> "LayoutAdaptiveDescription".GetLocalizedResource();

		public override bool IsExecutable
			=> Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph
			=> new("\uF576");

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

		public Task ExecuteAsync()
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

	internal sealed class LayoutDecreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IDisplayPageContext DisplayPageContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "DecreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutDecreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Subtract, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemMinus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			((DisplayPageContext.LayoutType == LayoutTypes.Details && UserSettingsService.LayoutSettingsService.DetailsViewSize > DetailsViewSizeKind.Compact) ||
			(DisplayPageContext.LayoutType == LayoutTypes.List && UserSettingsService.LayoutSettingsService.ListViewSize > ListViewSizeKind.Compact) ||
			(DisplayPageContext.LayoutType == LayoutTypes.Grid && UserSettingsService.LayoutSettingsService.GridViewSize > GridViewSizeKind.Small) ||
			(DisplayPageContext.LayoutType == LayoutTypes.Columns && UserSettingsService.LayoutSettingsService.ColumnsViewSize > ColumnsViewSizeKind.Compact));

		public LayoutDecreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			DisplayPageContext.PropertyChanged += DisplayPageContext_PropertyChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged += UserSettingsService_PropertyChanged;
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

		private void DisplayPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.LayoutType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void UserSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ILayoutSettingsService.DetailsViewSize):
				case nameof(ILayoutSettingsService.ListViewSize):
				case nameof(ILayoutSettingsService.GridViewSize):
				case nameof(ILayoutSettingsService.ColumnsViewSize):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		public Task ExecuteAsync()
		{
			switch (DisplayPageContext.LayoutType)
			{
				case LayoutTypes.Details:
					if (UserSettingsService.LayoutSettingsService.DetailsViewSize > DetailsViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.DetailsViewSize -= 1;
					break;
				case LayoutTypes.List:
					if (UserSettingsService.LayoutSettingsService.ListViewSize > ListViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.ListViewSize -= 1;
					break;
				case LayoutTypes.Tiles:
					break;
				case LayoutTypes.Grid:
					if (UserSettingsService.LayoutSettingsService.GridViewSize > GridViewSizeKind.Small)
						UserSettingsService.LayoutSettingsService.GridViewSize -= 1;
					break;
				case LayoutTypes.Columns:
					if (UserSettingsService.LayoutSettingsService.ColumnsViewSize > ColumnsViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.ColumnsViewSize -= 1;
					break;
			}

			return Task.CompletedTask;
		}
	}

	internal sealed class LayoutIncreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IDisplayPageContext DisplayPageContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "IncreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutIncreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Add, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemPlus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			((DisplayPageContext.LayoutType == LayoutTypes.Details && UserSettingsService.LayoutSettingsService.DetailsViewSize < DetailsViewSizeKind.ExtraLarge) ||
			(DisplayPageContext.LayoutType == LayoutTypes.List && UserSettingsService.LayoutSettingsService.ListViewSize < ListViewSizeKind.ExtraLarge) ||
			(DisplayPageContext.LayoutType == LayoutTypes.Grid && UserSettingsService.LayoutSettingsService.GridViewSize < GridViewSizeKind.ExtraLarge) ||
			(DisplayPageContext.LayoutType == LayoutTypes.Columns && UserSettingsService.LayoutSettingsService.ColumnsViewSize < ColumnsViewSizeKind.ExtraLarge));

		public LayoutIncreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			DisplayPageContext.PropertyChanged += DisplayPageContext_PropertyChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged += UserSettingsService_PropertyChanged;
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

		private void DisplayPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.LayoutType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void UserSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ILayoutSettingsService.DetailsViewSize):
				case nameof(ILayoutSettingsService.ListViewSize):
				case nameof(ILayoutSettingsService.GridViewSize):
				case nameof(ILayoutSettingsService.ColumnsViewSize):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		public Task ExecuteAsync()
		{
			switch (DisplayPageContext.LayoutType)
			{
				case LayoutTypes.Details:
					if (UserSettingsService.LayoutSettingsService.DetailsViewSize < DetailsViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.DetailsViewSize += 1;
					break;
				case LayoutTypes.List:
					if (UserSettingsService.LayoutSettingsService.ListViewSize < ListViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.ListViewSize += 1;
					break;
				case LayoutTypes.Tiles:
					break;
				case LayoutTypes.Grid:
					if (UserSettingsService.LayoutSettingsService.GridViewSize < GridViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.GridViewSize += 1;
					break;
				case LayoutTypes.Columns:
					if (UserSettingsService.LayoutSettingsService.ColumnsViewSize < ColumnsViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.ColumnsViewSize += 1;
					break;
			}

			return Task.CompletedTask;
		}
	}
}
