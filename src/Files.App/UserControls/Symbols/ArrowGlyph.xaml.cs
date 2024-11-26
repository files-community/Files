using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.UserControls.Symbols
{
	[DependencyProperty<double>("FontSize", DefaultValue = "(double)12.0")]
	[DependencyProperty<Brush>("Color", nameof(OnColorChange), DefaultValue = "null")]
	[DependencyProperty<ArrowSymbolType>("Arrow", nameof(OnArrowChange), DefaultValue = "Files.App.Data.Enums.ArrowSymbolType.ChevronRight")]
	[DependencyProperty<string>("Glyph", DefaultValue = "null")]
	public sealed partial class ArrowGlyph : UserControl, IRealTimeControl
	{
		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private string ForwardGlyph { get; } = Commands.NavigateForward.Glyph.BaseGlyph;

		private string BackGlyph { get; } = Commands.NavigateBack.Glyph.BaseGlyph;

		private string ChevronLeft { get; } = "\uE76B";
		private string ChevronRight { get; } = "\uE76C";

		private long _token;

		public ArrowGlyph()
		{
			InitializeComponent();
			InitializeContentLayout();
			UpdateGlyph();
			RealTimeLayoutService.AddCallback(this, UpdateGlyph);
		}

		private void UpdateGlyph()
		{
			Glyph = Arrow switch
			{
				ArrowSymbolType.Forward => FlowDirection == FlowDirection.LeftToRight ? ForwardGlyph : BackGlyph,
				ArrowSymbolType.Back => FlowDirection == FlowDirection.LeftToRight ? BackGlyph : ForwardGlyph,
				ArrowSymbolType.ChevronLeft => FlowDirection == FlowDirection.LeftToRight ? ChevronLeft : ChevronRight,
				ArrowSymbolType.ChevronRight => FlowDirection == FlowDirection.LeftToRight ? ChevronRight : ChevronLeft,
				_ => ChevronLeft
			};
		}

		private void OnColorChange(Brush oldValue, Brush newValue)
		{
			if (oldValue != newValue)
			{
				SymbolIcon.Foreground = newValue;
				UpdateGlyph();
			}
		}

		private void OnArrowChange(ArrowSymbolType oldValue, ArrowSymbolType newValue)
		{
			if (oldValue != newValue)
				UpdateGlyph();
		}
	}

}
