namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// INameTransform defines how file system names are transformed for use with archives, or vice versa.
	/// </summary>
	public interface INameTransform
	{
		/// <summary>
		/// Given a file name determine the transformed value.
		/// </summary>
		/// <param name="name">The name to transform.</param>
		/// <returns>The transformed file name.</returns>
		string TransformFile(string name);

		/// <summary>
		/// Given a directory name determine the transformed value.
		/// </summary>
		/// <param name="name">The name to transform.</param>
		/// <returns>The transformed directory name</returns>
		string TransformDirectory(string name);
	}
}
