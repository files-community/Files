namespace Files.Shared.Enums
{
    public enum LockType
    {
        /// <summary>A read lock, allowing multiple simultaneous reads</summary>
        Read,

        /// <summary>An upgradeable read, allowing multiple simultaneous reads, but with the potential that ths may be upgraded to a Write lock </summary>
        UpgradeableRead,

        /// <summary>A blocking Write lock</summary>
        Write
    }
}