namespace Files.Helpers
{
    public class ArrayHelpers
    {
        public static int FitBounds(int index, int length) =>
            index == 0 ? index : (index >= length ? length - 1 : (index < 0 ? 0 : index));
    }
}