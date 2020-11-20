﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls
{
    public sealed partial class PropertyListItem : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PropertyListItem), null);

        public string Text
        {
            get => GetValue(TextProperty) as string;
            set => SetValue(TextProperty, value as string);
        }

        public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register("ValueText", typeof(string), typeof(PropertyListItem), null);

        public string ValueText
        {
            get => GetValue(ValueTextProperty) as string;
            set => SetValue(ValueTextProperty, value as string);
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertyListItem), null);

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        private Button _ActionButton;
        public Button ActionButton
        {
            get => _ActionButton;
            set
            {
                _ActionButton = value;
                if(value != null)
                {
                    value.SetValue(Grid.ColumnProperty, 2);
                    MainGrid.Children.Add(value);
                }
            }

        }
        

        public PropertyListItem()
        {
            this.InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}