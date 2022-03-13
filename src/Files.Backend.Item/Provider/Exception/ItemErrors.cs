namespace Files.Backend.Item
{
    public enum ItemErrors : ushort
    {
        Unknown,
        Unauthorized,
        NotFound,
        InUse,
        NameTooLong,
        InProgress,
    }
}
