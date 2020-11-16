namespace Files.Commands
{
    public partial class ItemOperations
    {
        private IShellPage AppInstance = null;

        public ItemOperations(IShellPage appInstance)
        {
            AppInstance = appInstance;
        }
    }
}