using System.Text;

namespace Files.Helpers
{
    public static class PathHelpers
    {
        public static string Combine(this string path, params string[] paths)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(path);

            foreach (string item in paths)
            {
                sb.Append($"\\{item}");
            }

            string finalPath = sb.ToString();

            return finalPath.Replace("\\\\\\", "\\\\").Replace("\\\\", "\\");
        }
    }
}
