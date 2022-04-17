using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class SelectiveSerialization
    {
        private struct PropertyData
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        private struct SerializationData
        {
            public PropertyData[] Properties { get; set; }
            public Type[] KnownTypes { get; set; }
        }

        private static SerializationData ExtractSerializationData(object target)
        {
            List<PropertyData> serializableData = new List<PropertyData>();
            List<Type> knownTypes = new List<Type>();

            var properties = target.GetType().GetProperties();
            foreach (var property in properties)
            {
                var serializablePropertyAttributes = property.GetCustomAttributes<SelectiveSerializationProperty>();

                if (serializablePropertyAttributes.Count() > 0)
                {
                    knownTypes.Add(property.PropertyType);

                    foreach (var attribute in serializablePropertyAttributes)
                    {
                        knownTypes.AddRange(attribute.KnownTypes);
                    }

                    serializableData.Add(new PropertyData()
                    {
                        Name = property.Name,
                        Value = property.GetValue(target)
                    });
                }
            }

            return new SerializationData()
            {
                Properties = serializableData.ToArray(),
                KnownTypes = knownTypes.ToArray()
            };
        }

        public static string ToString(object target)
        {
            var serializationData = ExtractSerializationData(target);

            return new KnownTypeSerialization().SetKnownTypes(serializationData.KnownTypes)
                                               .Serialize(serializationData.Properties);
        }

        public static bool FromString<T>(ref T target, string serializedString)
        {
            bool isSuccess = false;

            try
            {
                var serializationData = ExtractSerializationData(target);

                var deseriaizedProperties = new KnownTypeSerialization().SetKnownTypes(serializationData.KnownTypes)
                                                                        .Deserialize<PropertyData[]>(serializedString);

                foreach (var deserializedProperty in deseriaizedProperties)
                {
                    if (serializationData.Properties.Any(w => w.Name == deserializedProperty.Name))
                    {
                        target.GetType().GetProperty(deserializedProperty.Name).SetValue(target, deserializedProperty.Value);
                    }
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return isSuccess;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectiveSerializationProperty : Attribute
    {
        public List<Type> KnownTypes { get; } = new List<Type>();

        public SelectiveSerializationProperty(Type[] knownTypes)
        {
            KnownTypes.AddRange(knownTypes);
        }

        public SelectiveSerializationProperty(Type knownType)
        {
            KnownTypes.Add(knownType);
        }

        public SelectiveSerializationProperty()
        {
        }
    }
}
