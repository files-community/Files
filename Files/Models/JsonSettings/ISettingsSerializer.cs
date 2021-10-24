using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Models.JsonSettings
{
    public interface ISettingsSerializer
    {
        bool WriteToFile(string json);

        string ReadFromFile();
    }
}
