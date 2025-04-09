// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text;

namespace Files.App.Data.Items
{
    /// <summary>
    /// Represents a text encoding in the application.
    /// </summary>
    public sealed class EncodingItem
    {

        public Encoding? Encoding { get; set; }

        /// <summary>
        /// Gets the encoding name. e.g. English (United States)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodingItem"/> class.
        /// </summary>
        /// <param name="code">The code of the language.</param>
        public EncodingItem(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                Encoding = null;
                Name = Strings.Default.GetLocalizedResource();
            }
            else
            {
                Encoding = Encoding.GetEncoding(code);
                Name = Encoding.EncodingName;
            }
        }

        public override string ToString() => Name;
    }
}
