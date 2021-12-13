using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Models.JsonSettings
{
    public interface IJsonSettingsSerializer
    {

        string SerializeToJson(object obj);

        T DeserializeFromJson<T>(string json);
    }
}
