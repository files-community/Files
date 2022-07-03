using Files.Shared.Enums;
using Windows.Storage;

namespace Files.Filesystem.Helpers
{
    public static class ConflitExtensions
    {
        public static NameCollisionOption Convert(this FileNameConflictResolveOptionType option) => option switch
        {
            FileNameConflictResolveOptionType.Skip => NameCollisionOption.FailIfExists,
            FileNameConflictResolveOptionType.GenerateNewName => NameCollisionOption.GenerateUniqueName,
            FileNameConflictResolveOptionType.ReplaceExisting => NameCollisionOption.ReplaceExisting,
            _ => NameCollisionOption.GenerateUniqueName,
        };
        public static FileNameConflictResolveOptionType ConvertBack(this NameCollisionOption option) => option switch
        {
            NameCollisionOption.FailIfExists => FileNameConflictResolveOptionType.Skip,
            NameCollisionOption.GenerateUniqueName => FileNameConflictResolveOptionType.GenerateNewName,
            NameCollisionOption.ReplaceExisting => FileNameConflictResolveOptionType.ReplaceExisting,
            _ => FileNameConflictResolveOptionType.GenerateNewName,
        };

        public static CreationCollisionOption Convert(this NameCollisionOption option) => option switch
        {
            NameCollisionOption.FailIfExists => CreationCollisionOption.FailIfExists,
            NameCollisionOption.GenerateUniqueName => CreationCollisionOption.GenerateUniqueName,
            NameCollisionOption.ReplaceExisting => CreationCollisionOption.ReplaceExisting,
            _ => CreationCollisionOption.GenerateUniqueName,
        };
    }
}
