namespace Files.Helpers
{
    public class EnumerableHelpers
    {
        public static int FitBounds(int index, int length) =>
            index >= length ? length - 1 : (index < 0 ? 0 : index);
    }
}
