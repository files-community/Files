using Files.Enums;
using Windows.Storage;

namespace Files.Helpers
{
    public static class EnumConversionHelpers
    {
        public static CreationCollisionOption Convert(this NameCollisionOption option)
        {
            switch (option)
            {
                case NameCollisionOption.GenerateUniqueName:
                    return CreationCollisionOption.GenerateUniqueName;

                case NameCollisionOption.ReplaceExisting:
                    return CreationCollisionOption.ReplaceExisting;

                case NameCollisionOption.FailIfExists:
                    return CreationCollisionOption.FailIfExists;

                default:
                    return CreationCollisionOption.GenerateUniqueName;
            }
        }

        public static NameCollisionOption Convert(this FileNameConflictResolveOptionType option)
        {
            switch (option)
            {
                case FileNameConflictResolveOptionType.GenerateNewName:
                    return NameCollisionOption.GenerateUniqueName;

                case FileNameConflictResolveOptionType.ReplaceExisting:
                    return NameCollisionOption.ReplaceExisting;

                case FileNameConflictResolveOptionType.Skip:
                    return NameCollisionOption.FailIfExists;

                default:
                    return NameCollisionOption.GenerateUniqueName;
            }
        }
    }
}