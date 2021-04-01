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
    }
}
