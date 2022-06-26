namespace Files.Uwp.Controllers
{
    internal interface IJson
    {
        string JsonFileName { get; }

        void SaveModel();
    }
}