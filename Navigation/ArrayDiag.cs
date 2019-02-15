using System.Diagnostics;

namespace Files.Navigation
{
    public class ArrayDiag
    {
        public static void DumpArray()
        {
            foreach (string s in History.HistoryList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }

        public static void DumpForwardArray()
        {
            foreach (string s in History.ForwardList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }
    }
}