using Microsoft.Toolkit.Mvvm.ComponentModel;
using SQLitePCL;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Filesystem
{
    /// <summary>
    /// This class is used to represent a file property from the Windows.Storage API
    /// </summary>
    public class FileProperty : ObservableObject
    {
        public string Name { get; set; } = "Test:";
        public string Property { get; set; }
        public string Section { get; set; }
        public object Value { get; set; }
        public IValueConverter Converter { get; set; }
        public bool IsReadOnly { get; set; } = true;
        public bool IsPersonalProperty { get; set; } = false;

        /// <summary>
        /// If the property is hidden, it is only shown when the user presses show all
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// If a property has an action to run on a button press, eg "Open in maps", define it's action here
        /// </summary>
        public Action ActionButton { get; set; }

        public Visibility Visibility { get; set; } = Visibility.Visible;

        public FileProperty()
        {

        }
    }
}