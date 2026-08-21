using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Files.App.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.Controls
{
	#region Template Parts

	[TemplatePart( Name = ToggleSwitchPartName , Type = typeof( ToggleSwitch ) )]

	#endregion



	public sealed partial class PropertiesViewItemToggle : PropertiesViewItem
	{
		#region Constants
		internal const string ToggleSwitchPartName = "PART_ToggleSwitch";

		#endregion




		#region Properties

		public bool IsToggled
		{
			get { return (bool)GetValue( IsToggledProperty ); }
			set { SetValue( IsToggledProperty , value ); }
		}

		public static readonly DependencyProperty IsToggledProperty =
			DependencyProperty.Register(
				"IsToggled",
				typeof(bool),
				typeof(PropertiesViewItemToggle),
				new PropertyMetadata( false , OnIsToggledPropertyChanged ) );

		#endregion




		#region Property Changed

		private static void OnIsToggledPropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItemToggle)d;
			control.UpdateIsToggledProperty( (bool)e.NewValue );
		}

		private void UpdateIsToggledProperty(bool isToggled)
		{
			
		}

		#endregion


		private bool _isLoaded;
		public PropertiesViewItemToggle()
		{
			DefaultStyleKey = typeof( PropertiesViewItemToggle );

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateIsToggledProperty( (bool)IsToggled );
			};
		}




		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}

	}
}
