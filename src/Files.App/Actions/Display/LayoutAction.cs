// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Details;

		public override string Label
			=> "Details".GetLocalizedResource();

		public override string Description
			=> "LayoutDetailsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconDetailsLayout");

		public override HotKey HotKey
			=> new(Keys.Number1, KeyModifiers.CtrlShift);
	}
	
	internal class LayoutListAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.List;

		public override string Label
			=> "List".GetLocalizedResource();

		public override string Description
			=> "LayoutListDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconListLayout");

		public override HotKey HotKey
			=> new(Keys.Number2, KeyModifiers.CtrlShift);
	}

	internal class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Tiles;

		public override string Label
			=> "Tiles".GetLocalizedResource();

		public override string Description
			=> "LayoutTilesDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconTilesLayout");

		public override HotKey HotKey
			=> new(Keys.Number3, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridSmallAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.GridSmall;

		public override string Label
			=> "SmallIcons".GetLocalizedResource();

		public override string Description
			=> "LayoutGridSmallDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconGridSmallLayout");

		public override HotKey HotKey
			=> new(Keys.Number4, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridMediumAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.GridMedium;

		public override string Label
			=> "MediumIcons".GetLocalizedResource();

		public override string Description
			=> "LayoutGridMediumDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconGridMediumLayout");

		public override HotKey HotKey
			=> new(Keys.Number5, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridLargeAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.GridLarge;

		public override string Label
			=> "LargeIcons".GetLocalizedResource();

		public override string Description
			=> "LayoutGridLargeDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconGridLargeLayout");

		public override HotKey HotKey
			=> new(Keys.Number6, KeyModifiers.CtrlShift);
	}

	internal class LayoutColumnsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Columns;

		public override string Label
			=> "Columns".GetLocalizedResource();

		public override string Description
			=> "LayoutColumnsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(opacityStyle: "ColorIconColumnsLayout");

		public override HotKey HotKey
			=> new(Keys.Number7, KeyModifiers.CtrlShift);
	}

	internal class LayoutAdaptiveAction : ToggleLayoutAction
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
			=> new(Keys.Number8, KeyModifiers.CtrlShift);

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

	internal class LayoutDecreaseSizeAction : IAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "DecreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutDecreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Subtract, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemMinus, KeyModifiers.Ctrl, false);

		public LayoutDecreaseSizeAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.DecreaseLayoutSize();

			return Task.CompletedTask;
		}
	}

	internal class LayoutIncreaseSizeAction : IAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "IncreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutIncreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Add, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemPlus, KeyModifiers.Ctrl, false);

		public LayoutIncreaseSizeAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.IncreaseLayoutSize();

			return Task.CompletedTask;
		}
	}
}
