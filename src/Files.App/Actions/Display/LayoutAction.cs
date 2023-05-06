// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Details;

		public override string Label { get; } = "Details".GetLocalizedResource();

		public override string Description => "LayoutDetailsDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE179");
		public override HotKey HotKey { get; } = new(Keys.Number1, KeyModifiers.CtrlShift);
	}

	internal class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Tiles;

		public override string Label { get; } = "Tiles".GetLocalizedResource();

		public override string Description => "LayoutTilesDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE15C");
		public override HotKey HotKey { get; } = new(Keys.Number2, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridSmallAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridSmall;

		public override string Label { get; } = "SmallIcons".GetLocalizedResource();

		public override string Description => "LayoutGridSmallDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE80A");
		public override HotKey HotKey { get; } = new(Keys.Number3, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridMediumAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridMedium;

		public override string Label { get; } = "MediumIcons".GetLocalizedResource();

		public override string Description => "LayoutGridMediumDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uF0E2");
		public override HotKey HotKey { get; } = new(Keys.Number4, KeyModifiers.CtrlShift);
	}

	internal class LayoutGridLargeAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridLarge;

		public override string Label { get; } = "LargeIcons".GetLocalizedResource();

		public override string Description => "LayoutGridLargeDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE739");
		public override HotKey HotKey { get; } = new(Keys.Number5, KeyModifiers.CtrlShift);
	}

	internal class LayoutColumnsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Columns;

		public override string Label { get; } = "Columns".GetLocalizedResource();

		public override string Description => "LayoutColumnsDescription".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconColumnsLayout");
		public override HotKey HotKey { get; } = new(Keys.Number6, KeyModifiers.CtrlShift);
	}

	internal class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Adaptive;

		public override string Label { get; } = "Adaptive".GetLocalizedResource();

		public override string Description => "LayoutAdaptiveDescription".GetLocalizedResource();

		public override bool IsExecutable => Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph { get; } = new("\uF576");
		public override HotKey HotKey { get; } = new(Keys.Number7, KeyModifiers.CtrlShift);

		protected override void OnContextChanged(string propertyName)
		{
			if (propertyName is nameof(IDisplayPageContext.IsLayoutAdaptiveEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal abstract class ToggleLayoutAction : ObservableObject, IToggleAction
	{
		protected IDisplayPageContext Context { get; } = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract LayoutTypes LayoutType { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }
		public abstract HotKey HotKey { get; }

		private bool isOn;
		public bool IsOn => isOn;

		public virtual bool IsExecutable => true;

		public ToggleLayoutAction()
		{
			isOn = Context.LayoutType == LayoutType;
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
				SetProperty(ref isOn, Context.LayoutType == LayoutType, nameof(IsOn));

			if (e.PropertyName is not null)
				OnContextChanged(e.PropertyName);
		}

		protected virtual void OnContextChanged(string propertyName) { }
	}

	internal class LayoutDecreaseSizeAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "DecreaseSize".GetLocalizedResource();

		public string Description => "LayoutDecreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Subtract, KeyModifiers.Ctrl);
		public HotKey MediaHotKey { get; } = new(Keys.OemMinus, KeyModifiers.Ctrl, false);

		public Task ExecuteAsync()
		{
			context.DecreaseLayoutSize();
			return Task.CompletedTask;
		}
	}

	internal class LayoutIncreaseSizeAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "IncreaseSize".GetLocalizedResource();

		public string Description => "LayoutIncreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Add, KeyModifiers.Ctrl);
		public HotKey MediaHotKey { get; } = new(Keys.OemPlus, KeyModifiers.Ctrl, false);

		public Task ExecuteAsync()
		{
			context.IncreaseLayoutSize();
			return Task.CompletedTask;
		}
	}
}
