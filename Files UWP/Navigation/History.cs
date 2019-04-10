using System.Collections.Generic;

namespace Files.Navigation
{
    public class History
    {
        // The list of paths previously navigated to 
        public static List<string> HistoryList = new List<string>();

        public static void AddToHistory(string pathToBeAdded)
        {
            // If HistoryList is currently less than 25 items  
            if (HistoryList.Count < 25)
            {
                // If there are items in HistoryList 
                if (HistoryList.Count > 0)
                {
                    // Make sure the item being added is not already added 
                    if (HistoryList[HistoryList.Count - 1] != pathToBeAdded)
                    {
                        HistoryList.Add(pathToBeAdded);
                    }
                }
                // If there are no items in HistoryList 
                else
                {
                    HistoryList.Add(pathToBeAdded);
                    
                }
            }
            // If History list is exactly 25 items (or greater) and the item being added is not already added 
            else if ((HistoryList.Count >= 25) && (HistoryList[HistoryList.Count - 1] != pathToBeAdded))
            {
                for (int i = 0; i < (HistoryList.Count - 1); i++)
                {
                    // Shift list contents left by one to delete first item, effectively making space for next item 
                    HistoryList[i] = HistoryList[i + 1];
                }
                // Add new item in freed spot 
                
                HistoryList[24] = pathToBeAdded;
                
            }
        }

        public static List<string> ForwardList = new List<string>();
        
        public static void AddToForwardList(string pathToBeAdded)
        {
            if (ForwardList.Count > 0)
            {
                if (ForwardList[ForwardList.Count - 1] != pathToBeAdded)
                {
                    //ForwardList.Add(pathToBeAdded);
                    ForwardList.Insert(0, pathToBeAdded);
                }
            }
            else
            {
                //ForwardList.Add(pathToBeAdded);
                ForwardList.Insert(0, pathToBeAdded);
            }
        }
    }
}