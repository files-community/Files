namespace Files.Backend.Services
{
    public interface IShortcutKeyService
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance can invoke shortcut keys.
        /// </summary>
        /// <value><c>true</c> if this instance can invoke shortcut keys; otherwise, <c>false</c>.</value>
        bool CanInvokeShortcutKeys { get; set; }
    }
}
