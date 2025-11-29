using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Files.App.UnitTests
{
    [TestClass]
    public class BulkConcurrentObservableCollectionTests
    {
        private class TestItem : INotifyPropertyChanged, Utils.Storage.IGroupableItem
        {
            public string Key { get; set; }

            private DateTimeOffset _date;
            public DateTimeOffset ItemDateModifiedReal
            {
                get => _date;
                set
                {
                    if (_date != value)
                    {
                        _date = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemDateModifiedReal)));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            public override string ToString() => ItemDateModifiedReal.ToString();
        }

        [TestMethod]
        public void When_ItemDateChanges_ItemMovesBetweenGroups()
        {
            // Group by logic: within 7 days = "Recent", else "Old"
            var col = new Utils.Storage.BulkConcurrentObservableCollection<TestItem>();
            col.ItemGroupKeySelector = item => (DateTimeOffset.Now - item.ItemDateModifiedReal).TotalDays <= 7 ? "Recent" : "Old";

            var recentItem = new TestItem { ItemDateModifiedReal = DateTimeOffset.Now.AddDays(-3) };
            var oldItem = new TestItem { ItemDateModifiedReal = DateTimeOffset.Now.AddDays(-400) };

            col.Add(recentItem);
            col.Add(oldItem);

            Assert.IsNotNull(col.GroupedCollection);
            Assert.AreEqual(2, col.GroupedCollection.Count);

            // Now change recentItem date so it becomes old
            recentItem.ItemDateModifiedReal = DateTimeOffset.Now.AddDays(-400);

            // It should have been moved to the old group
            var recentGroup = col.GroupedCollection.FirstOrDefault(g => g.Model.Key == "Recent");
            var oldGroup = col.GroupedCollection.FirstOrDefault(g => g.Model.Key == "Old");

            Assert.IsTrue(recentGroup == null || !recentGroup.Contains(recentItem), "recentItem should not be in recent group anymore");
            Assert.IsNotNull(oldGroup, "old group should exist");
            Assert.IsTrue(oldGroup.Contains(recentItem), "recentItem should be in old group now");
        }
    }
}
