namespace Files.App.Controllers
{
	internal interface IJson
	{
		string JsonFileName { get; }

		void SaveModel();
	}
}