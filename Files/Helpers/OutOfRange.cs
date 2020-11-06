namespace Files.Helpers
{
    public static class OutOfRange
    {
        public static int FitBounds(int index, int length) => index >= length ? length - 1 : (index < 0 ? 0 : index);
    }
}
