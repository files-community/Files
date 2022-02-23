namespace Files.Controllers
{
    internal interface IJson
    {
        string JsonFileName { get; }

        void SaveModel();
    }
}