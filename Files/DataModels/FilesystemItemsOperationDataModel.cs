using Files.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.DataModels
{
    public struct FilesystemItemsOperationDataModel
    {
        public FilesystemOperationType OperationType;

        public bool MustResolveConflicts;

        public bool PermanentlyDelete;

        public bool PermanentlyDeleteEnabled;

        /// <summary>
        /// The items that are copied/moved/deleted from the source directory (to destination)
        /// </summary>
        public Dictionary<string, string> IncomingItems;

        /// <summary>
        /// The items that are conflicting between <see cref="IncomingItems"/> and the items that are in the destination directory
        /// </summary>
        public Dictionary<string, string> ConflictingItems;

        public FilesystemItemsOperationDataModel(FilesystemOperationType operationType, bool mustResolveConflicts, bool permanentlyDelete, bool permanentlyDeleteEnabled, Dictionary<string, string> incomingItems, Dictionary<string, string> conflictingItems)
        {
            this.OperationType = operationType;
            this.MustResolveConflicts = mustResolveConflicts;
            this.PermanentlyDelete = permanentlyDelete;
            this.PermanentlyDeleteEnabled = permanentlyDeleteEnabled;
            this.IncomingItems = incomingItems;
            this.ConflictingItems = conflictingItems;
        }

        public override string ToString()
        {
            string operationName = string.Empty;

            switch (OperationType)
            {
                case FilesystemOperationType.Copy:
                    operationName = "COPY";
                    break;

                case FilesystemOperationType.Move:
                    operationName = "MOVE";
                    break;

                case FilesystemOperationType.Delete:
                    operationName = "DELETE";
                    break;
            }

            string serialized = string.Empty;

            List<string> serializedItems = new List<string>();
            Dictionary<string, string> nonConflictingItems = IncomingItems.Except(ConflictingItems).ToDictionary((kvp) => kvp.Key, (kvp) => kvp.Value);

            // Conflicting items
            foreach (var item in ConflictingItems)
            {
                serializedItems.Add($"CONFLICT {item.Key} -- {item.Value}");
            }

            // Non-conflicting items
            foreach (var item in nonConflictingItems)
            {
                if (OperationType == FilesystemOperationType.Delete)
                {
                    serializedItems.Add($"{operationName} {item.Key}");
                }
                else
                {
                    serializedItems.Add($"{operationName} {item.Key} -> NEW {item.Value}");
                }
            }

            // Create a string
            foreach (var item in serializedItems)
            {
                serialized += $"{item}\n";
            }

            return serialized;
        }
    }
}
