using System;
using Windows.UI.Xaml.Data;
using Files.Shared.Enums;

namespace Files.Uwp.ValueConverters.EnumConverters
{
    internal sealed class ConflictResolveOptionToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (FileNameConflictResolveOptionType)value switch
            {
                FileNameConflictResolveOptionType.None => 0,
                FileNameConflictResolveOptionType.GenerateNewName => 0,
                FileNameConflictResolveOptionType.ReplaceExisting => 1,
                FileNameConflictResolveOptionType.Skip => 2,
                _ => 0
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case 0:
                    return FileNameConflictResolveOptionType.GenerateNewName;

                case 1:
                    return FileNameConflictResolveOptionType.ReplaceExisting;

                case 2:
                    return FileNameConflictResolveOptionType.Skip;

                default:
                    return FileNameConflictResolveOptionType.None;
            }
        }
    }
}
