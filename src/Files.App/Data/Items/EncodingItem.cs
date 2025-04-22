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
        public EncodingItem(string? code)
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

		public EncodingItem(Encoding encoding, string name)
		{
			Encoding = encoding;
			Name = name;
		}

		public static EncodingItem[] Defaults = new string?[] {
			null,//System Default
            "UTF-8",
			
            //All possible Windows system encodings
            //reference: https://en.wikipedia.org/wiki/Windows_code_page
            //East Asian
            "shift_jis",       //Japanese
            "gb2312",          //Simplified Chinese
            "big5",            //Traditional Chinese
            "ks_c_5601-1987",  //Korean
            
            //Southeast Asian
            "Windows-1258",    //Vietnamese
            "Windows-874",     //Thai
            
            //Middle East
            "Windows-1256",    //Arabic
            "Windows-1255",    //Hebrew
            "Windows-1254",    //Turkish
            
            //European
            "Windows-1252",    //Western European
            "Windows-1250",    //Central European
            "Windows-1251",    //Cyrillic
            "Windows-1253",    //Greek
            "Windows-1257",    //Baltic
            
            "macintosh",
		}
			.Select(x => new EncodingItem(x))
			.ToArray();

		public override string ToString() => Name;
    }
}
