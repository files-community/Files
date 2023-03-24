﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Actions
{
	internal class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Details;

		public override string Label { get; } = "Details".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE179");
		public override HotKey HotKey { get; } = new(VirtualKey.Number1, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Tiles;

		public override string Label { get; } = "Tiles".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE15C");
		public override HotKey HotKey { get; } = new(VirtualKey.Number2, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutGridSmallAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridSmall;

		public override string Label { get; } = "SmallIcons".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE80A");
		public override HotKey HotKey { get; } = new(VirtualKey.Number3, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutGridMediumAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridMedium;

		public override string Label { get; } = "MediumIcons".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uF0E2");
		public override HotKey HotKey { get; } = new(VirtualKey.Number4, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutGridLargeAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.GridLarge;

		public override string Label { get; } = "LargeIcons".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new("\uE739");
		public override HotKey HotKey { get; } = new(VirtualKey.Number5, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutColumnsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Columns;

		public override string Label { get; } = "Columns".GetLocalizedResource();

		public override RichGlyph Glyph { get; } = new(opacityStyle: "ColorIconColumnsLayout");
		public override HotKey HotKey { get; } = new(VirtualKey.Number6, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);
	}

	internal class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType => LayoutTypes.Adaptive;

		public override string Label { get; } = "Adaptive".GetLocalizedResource();

		public override bool IsExecutable => Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph { get; } = new("\uF576");
		public override HotKey HotKey { get; } = new(VirtualKey.Number7, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift);

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

		public string Description => "TODO: Need to be described.";

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

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.Subtract, VirtualKeyModifiers.Control);
		public HotKey MediaHotKey { get; } = new((VirtualKey)189, VirtualKeyModifiers.Control);

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

		public string Description => "TODO: Need to be described.";

		public HotKey HotKey { get; } = new(VirtualKey.Add, VirtualKeyModifiers.Control);
		public HotKey MediaHotKey { get; } = new((VirtualKey)187, VirtualKeyModifiers.Control);

		public Task ExecuteAsync()
		{
			context.IncreaseLayoutSize();
			return Task.CompletedTask;
		}
	}
}
