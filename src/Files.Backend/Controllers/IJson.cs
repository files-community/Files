namespace Files.Backend.Controllers
{
	public interface IJson
	{
		string JsonFileName { get; }

		void SaveModel();
	}
}
