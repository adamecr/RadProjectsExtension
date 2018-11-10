using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace net.adamec.dev.vs.extension.radprojects.checklists
{
    /// <inheritdoc />
    /// <summary>
    /// Container of the available checklists
    /// </summary>
    public class Checklists : INotifyPropertyChanged
    {
        private Checklist current;
        /// <summary>
        /// Current checklist
        /// </summary>
        public Checklist Current
        {
            get => current;
            set { current = value; NotifyPropertyChanged(nameof(Current)); }
        }

        /// <summary>
        /// List of the checklists available
        /// </summary>
        /// <remarks>Note: Currently, it's not expected that the collection will change during the processing of the checklist</remarks>
        public ObservableCollection<Checklist> Items { get; set; } = new ObservableCollection<Checklist>();

        /// <inheritdoc />
        /// <summary>
        /// Property Changed event - to be raised in property setters to notify the UI about the changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">Name of the property changed</param>
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Loads all checklists (.chklist) files from the given location
        /// </summary>
        /// <param name="directory">Root directory</param>
        /// <param name="recursive">Flag whether to check also in subdirectories (default=true)</param>
        /// <returns><see cref="Checklists"/> object containing loaded checklists</returns>
        public static Checklists Load(string directory, bool recursive = true)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException(nameof(directory));
            if (!Directory.Exists(directory)) throw new Exception($"Directory {directory} doesn't exist");

            var checklistFiles = new DirectoryInfo(directory).GetFiles("*.chklist", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            var retVal = new Checklists();
            foreach (var fileInfo in checklistFiles)
            {
                var checkList = Checklist.Load(fileInfo.FullName);
                if (checkList != null)
                    retVal.Items.Add(checkList);
            }

            if (retVal.Items.Count > 0)
                retVal.Current = retVal.Items[0];
            return retVal;
        }
    }

}
