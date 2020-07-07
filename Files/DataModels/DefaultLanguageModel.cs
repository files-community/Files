using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.DataModels
{
    public class DefaultLanguageModel
    {
        public string ID;
        public string Name;

        public DefaultLanguageModel()
        {
            Name = "";
            ID = "";
        }

        public DefaultLanguageModel(string id)
        {
            var info = new CultureInfo(id);
            ID = info.Name;
            Name = info.DisplayName;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
