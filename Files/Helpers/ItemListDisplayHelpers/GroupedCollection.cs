using Files.Filesystem;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace Files.Helpers
{
    public class GroupedCollection<T> : BulkConcurrentObservableCollection<T>, IGroupedCollectionHeader
    {
        public GroupedHeaderViewModel Model { get; set; }

        public GroupedCollection(IEnumerable<T> items) : base(items)
        {
            AddEvents();
        }

        public GroupedCollection(string key) : base()
        {
            AddEvents();
            Model = new GroupedHeaderViewModel()
            {
                Key = key,
                Text = key,
            };
        }
        
        public GroupedCollection(string key, string text) : base()
        {
            AddEvents();
            Model = new GroupedHeaderViewModel()
            {
                Key = key,
                Text = text,
            };
        }

        private void AddEvents()
        {
            PropertyChanged += GroupedCollection_PropertyChanged;
        }

        private void GroupedCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Count))
            {
                Model.CountText = $"{Count} items";
            }
        }

        public async Task InitializeExtendedGroupHeaderInfoAsync()
        {
            if(GetExtendedGroupHeaderInfo is null)
            {
                return;
            }

            Model.PausePropertyChangedNotifications();

            GetExtendedGroupHeaderInfo.Invoke(this);
            await UpdateModelAsync();

            Model.Initialized = true;
        }

        public async Task UpdateModelAsync()
        {
            await CoreApplication.MainView.DispatcherQueue.EnqueueAsync(() =>
            {
                Model.ResumePropertyChangedNotifications();
            }, Windows.System.DispatcherQueuePriority.Low);
        }
    }

    /// <summary>
    /// This interface is used to allow using x:Bind for the group header template.
    /// <br/>
    /// This is needed because x:Bind does not work with generic types, however it does work with interfaces.
    /// that are implemented by generic types.
    /// </summary>
    public interface IGroupedCollectionHeader
    {
        public GroupedHeaderViewModel Model { get; set; }
    }

    public class GroupedHeaderViewModel : ObservableObject
    {
        public string Key { get; set; }
        public bool Initialized { get; set; }

        private string text;
        public string Text
        {
            get => text;
            set => SetPropertyWithUpdateDelay(ref text, value);
        }


        private string subtext;
        public string Subtext
        {
            get => subtext;
            set => SetPropertyWithUpdateDelay(ref subtext, value);
        }

        private string countText;
        public string CountText
        {
            get => countText;
            set => SetPropertyWithUpdateDelay(ref countText, value);
        }

        private void SetPropertyWithUpdateDelay<T>(ref T field, T newVal, [CallerMemberName] string propName = null)
        {
            if (propName is null)
            {
                return;
            }
            var name = propName.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase)
                ? propName.Substring(4)
                : propName;

            if (!deferPropChangedNotifs)
            {
                SetProperty<T>(ref field, newVal, name);
            }
            else
            {
                field = newVal;
                if (!changedPropQueue.Contains(name))
                {
                    changedPropQueue.Add(name);
                }
            }
        }

        public void PausePropertyChangedNotifications()
        {
            deferPropChangedNotifs = true;
        }

        public void ResumePropertyChangedNotifications()
        {
            if(deferPropChangedNotifs == false)
            {
                return;
            }
            deferPropChangedNotifs = false;
            changedPropQueue.ForEach(prop => OnPropertyChanged(prop));
            changedPropQueue.Clear();
        }

        private List<string> changedPropQueue = new List<string>();

        // This is true by default to make it easier to initialize groups from a different thread
        private bool deferPropChangedNotifs = true;
    }
}
