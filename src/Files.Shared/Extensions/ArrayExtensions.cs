using System;

namespace Files.Shared.Extensions
{
    public static class ArrayExtensions
    {
        public static T[] CloneArray<T>(this T[] array)
        {
            var clonedArray = new T[array.Length];
            Array.Copy(array, 0, clonedArray, 0, array.Length);

            return clonedArray;
        }
    }
}
