using System.Runtime.InteropServices;
using Windows.Security.Credentials;

namespace Files.App.Helpers
{
	internal class CredentialsHelpers
	{
		public static void SavePassword(string resourceName, string username, string password)
		{
			var vault = new PasswordVault();
			var credential = new PasswordCredential(resourceName, username, password);

			vault.Add(credential);
		}

		// Remove saved credentials from the vault
		public static void DeleteSavedPassword(string resourceName, string username)
		{
			var vault = new PasswordVault();
			var credential = vault.Retrieve(resourceName, username);

			vault.Remove(credential);
		}

		public static string GetPassword(string resourceName, string username)
		{
			try
			{
				var vault = new PasswordVault();
				var credential = vault.Retrieve(resourceName, username);

				credential.RetrievePassword();

				return credential.Password;
			}
			// Thrown if the resource does not exist
			catch (COMException)
			{
				return string.Empty;
			}
		}
	}
}
