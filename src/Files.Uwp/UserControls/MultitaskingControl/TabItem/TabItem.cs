using Files.ViewModels;
using Files.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Files.UserControls.MultitaskingControl
{
    public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
    {
        private string header;

        public string Header
        {
            get => header;
            set => SetProperty(ref header, value);
        }

        private string description;

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private IconSource iconSource;

        public IconSource IconSource
        {
            get => iconSource;
            set => SetProperty(ref iconSource, value);
        }

        public TabItemControl Control { get; private set; }

        private bool allowStorageItemDrop;

        public bool AllowStorageItemDrop
        {
            get => allowStorageItemDrop;
            set => SetProperty(ref allowStorageItemDrop, value);
        }

        private bool isPinned = false;

        [SerializableProperty]
        public bool IsPinned
        {
            get => isPinned;
            set => SetProperty(ref isPinned, value);
        }

        private TabItemArguments tabItemArguments;

        [SerializableProperty(typeof(PaneNavigationArguments))]
        public TabItemArguments TabItemArguments
        {
            get => Control?.NavigationArguments ?? tabItemArguments;
            set
            {
                tabItemArguments = value;

                if (Control != null)
                {
                    Control.NavigationArguments = tabItemArguments;
                }
            }
        }

        public TabItem()
        {
            Control = new TabItemControl();
        }

        public void Unload()
        {
            Control.ContentChanged -= MainPageViewModel.Control_ContentChanged;
            tabItemArguments = Control?.NavigationArguments;
            Dispose();
        }

        #region IDisposable

        public void Dispose()
        {
            Control?.Dispose();
            Control = null;
        }

        #endregion IDisposable
    }

    public class KnownTypeSerialization
    {
        private KnownTypesBinder knownTypesBinder = null;

        public KnownTypeSerialization()
        {
            knownTypesBinder = new KnownTypesBinder
            {
                KnownTypes = new List<Type>() { }
            };
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

    public class TabItemArguments
    {
        public Type InitialPageType { get; set; }
        public object NavigationArg { get; set; }

        private static Type[] serializationKnownTypes = new Type[]
        {
            typeof(PaneNavigationArguments)
        };

        public string Serialize()
        {
            return new KnownTypeSerialization().SetKnownTypes(serializationKnownTypes)
                                               .Serialize(this);
        }

        public static TabItemArguments Deserialize(string obj)
        {
            return new KnownTypeSerialization().SetKnownTypes(serializationKnownTypes)
                                               .Deserialize<TabItemArguments>(obj);
        }
    }

    public class PropertySerializer<T>
    {
        private struct SerializablePropertyData
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        private struct SerializationData
        {
            public SerializablePropertyData[] Properties { get; set; }
            public Type[] KnownTypes { get; set; }
        }

        private static SerializationData ExtractSerializationData(T target)
        {
            List<SerializablePropertyData> serializableData = new List<SerializablePropertyData>();
            List<Type> knownTypes = new List<Type>();

            var properties = target.GetType().GetProperties();
            foreach (var property in properties)
            {
                var serializablePropertyAttributes = property.GetCustomAttributes<SerializableProperty>();

                if (serializablePropertyAttributes.Count() > 0)
                {
                    knownTypes.Add(property.PropertyType);

                    foreach(var attribute in serializablePropertyAttributes)
                    {
                        knownTypes.AddRange(attribute.KnownTypes);
                    }

                    serializableData.Add(new SerializablePropertyData()
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

        public static string ToString(T target)
        {
            var serializationData = ExtractSerializationData(target);

            return new KnownTypeSerialization().SetKnownTypes(serializationData.KnownTypes)
                                               .Serialize(serializationData.Properties);
        }

        public static bool FromString(ref T target, string targetString)
        {
            bool isSuccess = false;

            try
            {
                var serializationData = ExtractSerializationData(target);

                var deseriaizedProperties = new KnownTypeSerialization().SetKnownTypes(serializationData.KnownTypes)
                                                                        .Deserialize<SerializablePropertyData[]>(targetString);

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
    public class SerializableProperty : Attribute
    {
        public List<Type> KnownTypes { get; } = new List<Type>();

        public SerializableProperty(Type[] knownTypes)
        {
            KnownTypes.AddRange(knownTypes);
        }

        public SerializableProperty(Type knownType)
        {
            KnownTypes.Add(knownType);
        }

        public SerializableProperty()
        {

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