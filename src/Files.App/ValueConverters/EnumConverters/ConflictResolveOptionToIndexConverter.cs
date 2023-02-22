using Files.Core.Enums;
using Microsoft.UI.Xaml.Data;
using System;

namespace Files.App.ValueConverters.EnumConverters
{
	internal sealed class ConflictResolveOptionToIndexConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (FileNameConflictResolveOptionType)value switch
			{
				FileNameConflictResolveOptionType.None => -1,
				FileNameConflictResolveOptionType.GenerateNewName => 0,
				FileNameConflictResolveOptionType.ReplaceExisting => 1,
				FileNameConflictResolveOptionType.Skip => 2,
				_ => -1
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value switch
			{
				0 => FileNameConflictResolveOptionType.GenerateNewName,
				1 => FileNameConflictResolveOptionType.ReplaceExisting,
				2 => FileNameConflictResolveOptionType.Skip,
				_ => FileNameConflictResolveOptionType.None
			};
		}
	}
}
