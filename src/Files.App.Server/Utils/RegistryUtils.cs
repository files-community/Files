using Microsoft.Win32;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Files.App.Server.Utils
{
	internal sealed class RegistryUtils
	{
		internal static string CombineKeys(string key1, string key2)
		{
			key1 = key1.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');
			key2 = key2.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');
			return $"{key1}\\{key2}";
		}

		internal static string CombineKeys(string key1, string key2, string key3)
		{
			key1 = key1.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');
			key2 = key2.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');
			key3 = key3.Replace('/', '\\').TrimStart('\\').TrimEnd('\\');
			return $"{key1}\\{key2}\\{key3}";
		}

		internal static void BindValues(RegistryKey key, object? target, string namePrefix = "")
		{
			if (target is null)
			{
				return;
			}

			foreach (var p in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if ((p.PropertyType == typeof(string)
					|| p.PropertyType == typeof(string[])
					|| p.PropertyType.IsEnum
					|| p.PropertyType.IsPrimitive
					|| (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
					&& p.CanWrite)
				{
					var value = key.GetValue($"{namePrefix}{p.Name}");
					if (value is int intValue)
					{
						if (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?))
						{
							p.SetValue(target, intValue is 1);
						}
						else if (p.PropertyType == typeof(float) || p.PropertyType == typeof(float?))
						{
							p.SetValue(target, Unsafe.As<int, float>(ref intValue));
						}
						else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
						{
							p.SetValue(target, value);
						}
						else if (p.PropertyType == typeof(uint) || p.PropertyType == typeof(uint?))
						{
							p.SetValue(target, Unsafe.As<int, uint>(ref intValue));
						}
						else
						{
							throw new NotSupportedException($"Cannot assign a value of {value.GetType()} to {p}");
						}
					}
					else if (value is long longValue)
					{
						if (p.PropertyType == typeof(double) || p.PropertyType == typeof(double?))
						{
							p.SetValue(target, Unsafe.As<long, double>(ref longValue));
						}
						else if (p.PropertyType == typeof(long) || p.PropertyType == typeof(long?))
						{
							p.SetValue(target, value);
						}
						else if (p.PropertyType == typeof(ulong) || p.PropertyType == typeof(ulong?))
						{
							p.SetValue(target, Unsafe.As<long, ulong>(ref longValue));
						}
						else
						{
							throw new NotSupportedException($"Cannot assign a value of {value.GetType()} to {p}");
						}
					}
					else if (value is string stringValue)
					{
						if (p.PropertyType == typeof(string))
						{
							p.SetValue(target, stringValue);
						}
						else if (p.PropertyType.IsEnum)
						{
							p.SetValue(target, Enum.Parse(p.PropertyType, stringValue));
						}
						else
						{
							throw new NotSupportedException($"Cannot assign a value of {value.GetType()} to {p}");
						}
					}
					else if (value is string[] stringArray)
					{
						if (p.PropertyType == typeof(string[]))
						{
							p.SetValue(target, stringArray);
						}
						else
						{
							throw new NotSupportedException($"Cannot assign a value of {value.GetType()} to {p}");
						}
					}
					else if (value is null)
					{
						if (p.PropertyType == typeof(string) || (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
						{
							p.SetValue(target, null);
						}
					}
					else
					{
						throw new NotSupportedException($"Cannot assign a value of {value.GetType()} to {p}");
					}
				}
				else
				{
					BindValues(key, p.GetValue(target), $"{namePrefix}{p.Name}.");
				}
			}
		}

		internal static void SaveValues(RegistryKey key, object? source, string namePrefix = "")
		{
			if (source is null)
			{
				foreach (var name in key.GetValueNames())
				{
					if (name.StartsWith(namePrefix, StringComparison.Ordinal))
					{
						key.DeleteValue(name, false);
					}
				}

				return;
			}

			foreach (var p in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if ((p.PropertyType == typeof(string)
					|| p.PropertyType == typeof(string[])
					|| p.PropertyType.IsEnum
					|| p.PropertyType.IsPrimitive
					|| (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
					&& p.CanWrite)
				{
					var value = p.GetValue(source);
					switch (value)
					{
						case int intValue:
							key.SetValue($"{namePrefix}{p.Name}", intValue, RegistryValueKind.DWord);
							break;
						case uint uintValue:
							key.SetValue($"{namePrefix}{p.Name}", Unsafe.As<uint, int>(ref uintValue), RegistryValueKind.DWord);
							break;
						case long longValue:
							key.SetValue($"{namePrefix}{p.Name}", longValue, RegistryValueKind.QWord);
							break;
						case ulong ulongValue:
							key.SetValue($"{namePrefix}{p.Name}", Unsafe.As<ulong, long>(ref ulongValue), RegistryValueKind.QWord);
							break;
						case bool boolValue:
							key.SetValue($"{namePrefix}{p.Name}", boolValue ? 1 : 0, RegistryValueKind.DWord);
							break;
						case float floatValue:
							key.SetValue($"{namePrefix}{p.Name}", Unsafe.As<float, int>(ref floatValue), RegistryValueKind.DWord);
							break;
						case double doubleValue:
							key.SetValue($"{namePrefix}{p.Name}", Unsafe.As<double, long>(ref doubleValue), RegistryValueKind.QWord);
							break;
						case string stringValue:
							key.SetValue($"{namePrefix}{p.Name}", stringValue, RegistryValueKind.String);
							break;
						case string[] stringArray:
							key.SetValue($"{namePrefix}{p.Name}", stringArray, RegistryValueKind.MultiString);
							break;
						case null:
							key.DeleteValue($"{namePrefix}{p.Name}", false);
							break;
						default:
							if (p.PropertyType.IsEnum)
							{
								key.SetValue($"{namePrefix}{p.Name}", value.ToString()!, RegistryValueKind.String);
							}
							else
							{
								throw new NotSupportedException($"Cannot save the value of {p}");
							}
							break;
					}
				}
				else
				{
					SaveValues(key, p.GetValue(source), $"{namePrefix}{p.Name}.");
				}
			}
		}
	}
}
