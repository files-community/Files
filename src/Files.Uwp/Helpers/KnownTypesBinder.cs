using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Files.Uwp.Helpers
{
    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; } = new List<Type>();

        public Type BindToType(string assemblyName, string typeName)
        {
            if (!KnownTypes.Any(x => x.Name == typeName || x.FullName == typeName))
            {
                throw new ArgumentException();
            }
            else
            {
                return KnownTypes.SingleOrDefault(t => t.Name == typeName || t.FullName == typeName);
            }
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
}
