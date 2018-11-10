using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace net.adamec.dev.vs.extension.radprojects.checklists
{
    /// <inheritdoc />
    /// <summary>
    /// Check list definition
    /// </summary>
    public class Checklist : INotifyPropertyChanged
    {
        private string fileName;
        /// <summary>
        /// Name of the file, the checklist was loaded from by <see cref="Load"/> method. Used to (auto)save the changes (status/progress updates)
        /// </summary>
        [JsonIgnore]
        public string FileName
        {
            get => fileName;
            private set { fileName = value; NotifyPropertyChanged(nameof(FileName), false); }
        }

        private string name;
        /// <summary>
        /// Name of the checklist
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get => name;
            set { name = value; NotifyPropertyChanged(nameof(Name)); }
        }

        private bool isInProgress;
        /// <summary>
        /// Flag whether the check list is in progress (there is a step with status Active, Running or Evaluate)
        /// </summary>
        [JsonIgnore]
        public bool IsInProgress
        {
            get => isInProgress;
            private set { isInProgress = value; NotifyPropertyChanged(nameof(IsInProgress), false); }
        }

        /// <summary>
        /// List of the steps within the check list
        /// The collection changes are observed in <see cref="ItemsCollectionChanged"/>    
        /// The property changes of individual steps are observed in <see cref="ItemPropertyChanged"/>
        /// </summary>
        [JsonProperty("items")]
        public ObservableCollection<ChecklistItem> Items { get; set; } = new ObservableCollection<ChecklistItem>();

        /// <inheritdoc />
        /// <summary>
        /// Property Changed event - to be raised in property setters to notify the UI about the changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// CTOR
        /// </summary>
        public Checklist()
        {
            Items.CollectionChanged += ItemsCollectionChanged;
        }

        /// <summary>
        /// Raise the <see cref="PropertyChanged"/> event and saves the checklist (if not explicitly skipped) on change
        /// </summary>
        /// <param name="propertyName">Name of the property changed</param>
        /// <param name="save"/> Save the checklist (default=true - save at each change). Use false for JsonIgnored properties not to force unnecessary saves
        private void NotifyPropertyChanged(string propertyName, bool save = true)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (save) Save();
        }

        /// <summary>
        /// Track the changes of the checklist items collection - used to (un)subscribe to PropertyChanged event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void ItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Unsubscribe items' PropertyChanged event
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                    item.PropertyChanged -= ItemPropertyChanged;
            }
            //Subscribe items' PropertyChanged event
            // ReSharper disable once InvertIf
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += ItemPropertyChanged;
            }
        }

        /// <summary>
        /// Track the property changes of checklist items (steps)
        /// Used to set correct <see cref="IsInProgress"/> value (there is a step with status Active, Running or Evaluate)
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event data</param>
        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CheckAndSetInProgress();
            Save();
        }

        

        /// <summary>
        /// Provides the short string description of checklist - name and the number of steps
        /// </summary>
        /// <returns>Short string description of checklist</returns>
        public override string ToString()
        {
            return $"{Name} ({(Items == null || Items.Count == 0 ? "empty" : Items.Count + " items")})";
        }

        /// <summary>
        /// Starts the execution of the check list - resets the checklist and sets the first step to Active status
        /// </summary>
        public void Start()
        {
            if (Items == null || Items.Count <= 0) return;

            Reset();
            Items[0].Status = ChecklistItemStatusEnum.Active;
        }

        /// <summary>
        /// Resets the checklist - sets all items to Pending status
        /// </summary>
        public void Reset()
        {
            if (Items == null) return;

            foreach (var item in Items.Where(i => i.Status != ChecklistItemStatusEnum.Pending))
            {
                item.Status = ChecklistItemStatusEnum.Pending;
            }
        }

        /// <summary>
        /// Loads the <see cref="Checklist"/> from given <paramref name="fileName">file</paramref>
        /// </summary>
        /// <param name="fileName">Full path to the file to load the checklist from</param>
        /// <returns>Instance of <see cref="Checklist"/> loaded from <paramref name="fileName">file</paramref></returns>
        public static Checklist Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (!File.Exists(fileName)) throw new Exception($"Checklist file {fileName} doesn't exist");

            var checklist = JsonConvert.DeserializeObject<Checklist>(File.ReadAllText(fileName));
            checklist.CheckAndSetInProgress();
            checklist.FileName = fileName; //Must be empty (JsonIgnore) while loading to prevent saving on property change!!!
            
            return checklist;
        }

        /// <summary>
        /// Saves the current <see cref="Checklist"/> to <see cref="FileName"/> defined in checklist object
        /// </summary>
        public void Save()
        {
            if (!string.IsNullOrEmpty(FileName) && File.Exists(FileName))
                Save(FileName);
        }

        /// <summary>
        /// Saves the current <see cref="Checklist"/> to given <paramref name="fileFullName">file</paramref>
        /// </summary>
        /// <param name="fileFullName">Full path to the file to save the checklist to</param>
        public void Save(string fileFullName)
        {
            if (string.IsNullOrEmpty(fileFullName)) throw new ArgumentNullException(nameof(fileFullName));

            var templateInfoContent = JsonConvert.SerializeObject(this);
            File.WriteAllText(fileFullName, templateInfoContent);
        }
        /// <summary>
        /// Checks whether there is any item in progress status and sets the <see cref="IsInProgress"/> property accordingly
        /// </summary>
        private void CheckAndSetInProgress()
        {
            var inProgress = Items.Any(i =>
                i.Status == ChecklistItemStatusEnum.Active ||
                i.Status == ChecklistItemStatusEnum.Evaluate ||
                i.Status == ChecklistItemStatusEnum.Running);
            IsInProgress = inProgress;
        }
    }
}
