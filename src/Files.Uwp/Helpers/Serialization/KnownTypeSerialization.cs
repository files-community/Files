using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class KnownTypeSerialization
    {
        private KnownTypesBinder knownTypesBinder = new KnownTypesBinder
        {
            KnownTypes = new Type[] { }
        };

        public KnownTypeSerialization()
        {
            knownTypesBinder = new KnownTypesBinder
            {
                KnownTypes = new List<Type>() { }
            };
        }

        public KnownTypeSerialization(Type type)
        {
            SetKnownType(type);
        }

        public KnownTypeSerialization(Type[] types)
        {
            SetKnownTypes(types);
        }

        public string Serialize<T>(T target) => JsonConvert.SerializeObject(target, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = knownTypesBinder
        });

        public T Deserialize<T>(string obj) => JsonConvert.DeserializeObject<T>(obj, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = knownTypesBinder
        });

        public KnownTypeSerialization SetKnownTypes(Type[] types)
        {
            knownTypesBinder = new KnownTypesBinder
            {
                KnownTypes = types
            };

            return this;
        }

        public KnownTypeSerialization SetKnownType(Type type)
        {
            knownTypesBinder = new KnownTypesBinder
            {
                KnownTypes = new List<Type>() { type }
            };

            return this;
        }
    }

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName)
        {
            if (!KnownTypes.Any(x => x.Name == typeName))
            {
                throw new ArgumentException();
            }
            else
            {
                return KnownTypes.SingleOrDefault(t => t.Name == typeName);
            }
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
}
