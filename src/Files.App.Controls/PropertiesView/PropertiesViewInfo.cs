using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Files.App.Controls
{
    #region Template
        [TemplatePart( Name = InfoIconPresenterHolderName , Type = typeof( Viewbox ) )]

    [TemplateVisualState( Name = NormalStateName , GroupName = CommonStatesName )]
    [TemplateVisualState( Name = PointerOverStateName , GroupName = CommonStatesName )]
    [TemplateVisualState( Name = PressedStateName , GroupName = CommonStatesName )]
    [TemplateVisualState( Name = DisabledStateName , GroupName = CommonStatesName )]
    #endregion
    public partial class PropertiesViewInfo : Button
    {
        #region Constants
        private const string CommonStatesName = "CommonStates";
        private const string NormalStateName = "Normal";
        private const string PointerOverStateName = "PointerOver";
        private const string PressedStateName = "Pressed";
        private const string DisabledStateName = "Disabled";

        private const string InfoIconPresenterHolderName = "PART_InfoIconPresenterHolder";
        #endregion



        #region Properties

        [GeneratedDependencyProperty]
        public partial IconElement? InfoIcon { get; set; }
        #endregion




        public PropertiesViewInfo()
        {
            DefaultStyleKey = typeof( PropertiesViewInfo );
            InfoIcon = new FontIcon { Glyph = "\uEA80" , MirroredWhenRightToLeft = true  };
        }



        protected override void OnApplyTemplate()
        {
            IsEnabledChanged -= OnIsEnabledChanged;

            base.OnApplyTemplate();

            UpdateInfoIcon();

            IsEnabledChanged += OnIsEnabledChanged;
        }




        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed( e );
            VisualStateManager.GoToState( this , PressedStateName , true );
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased( e );
            VisualStateManager.GoToState( this , NormalStateName , true );
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered( e );
            VisualStateManager.GoToState( this , PointerOverStateName , true );
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited( e );
            VisualStateManager.GoToState( this , NormalStateName , true );
        }

        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost( e );
            VisualStateManager.GoToState( this , NormalStateName , true );
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled( e );
            VisualStateManager.GoToState( this , NormalStateName , true );
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown( e );
        }

        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            base.OnKeyUp( e );
        }
        
        
        

        partial void OnInfoIconChanged(IconElement? newValue)
        {
            UpdateInfoIcon();
        }

        private void UpdateInfoIcon()
        {
            if ( GetTemplateChild( InfoIconPresenterHolderName ) is FrameworkElement presenter )
                presenter.Visibility = InfoIcon is not null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }




        private void OnIsEnabledChanged(object sender , DependencyPropertyChangedEventArgs e)
        {
            UpdateCommonState( true );
        }


        private void UpdateCommonState(bool useTransitions)
        {
            VisualStateManager.GoToState( this , IsEnabled ? NormalStateName : DisabledStateName , useTransitions );
        }
    }
}
