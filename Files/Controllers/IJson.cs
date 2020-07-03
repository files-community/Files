namespace Files.Controllers
{
    interface IJson
    {
        string JsonFileName { get; }

        void Init();

        void SaveModel();
    }
}