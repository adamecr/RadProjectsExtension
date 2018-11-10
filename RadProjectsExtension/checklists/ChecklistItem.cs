using System.ComponentModel;
using Newtonsoft.Json;

namespace net.adamec.dev.vs.extension.radprojects.checklists
{
    /// <inheritdoc />
    /// <summary>
    /// Single step item within the checklist
    /// </summary>
    public class ChecklistItem : INotifyPropertyChanged
    {
        private string name;
        /// <summary>
        /// Name of the step (can also represent the short description/task)
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get => name;
            set { name = value; NotifyPropertyChanged(nameof(Name)); }
        }

        private string description;
        /// <summary>
        /// Optional description or work instructions
        /// </summary>
        [JsonProperty("description")]
        public string Description
        {
            get => description;
            set { description = value; NotifyPropertyChanged(nameof(Description)); }
        }

        private ChecklistItemTypeEnum itemType = ChecklistItemTypeEnum.Manual;
        /// <summary>
        /// Type of the step (manual/automated)
        /// </summary>
        [JsonProperty("type")]
        public ChecklistItemTypeEnum ItemType
        {
            get => itemType;
            set { itemType = value; NotifyPropertyChanged(nameof(ItemType)); }
        }

        private string command;
        /// <summary>
        /// Command to run for the automated steps
        /// </summary>
        [JsonProperty("command")]
        public string Command
        {
            get => command;
            set { command = value; NotifyPropertyChanged(nameof(Command)); }
        }

        private string commandArgs;
        /// <summary>
        /// Command arguments for the automated steps
        /// Variables "%Major%.%Minor%.%Patch%.%Build% should be supported by the executor
        /// </summary>
        [JsonProperty("commandArgs")]
        public string CommandArgs
        {
            get => commandArgs;
            set { commandArgs = value; NotifyPropertyChanged(nameof(CommandArgs)); }
        }

        private ChecklistItemStatusEnum status = ChecklistItemStatusEnum.Pending;
        /// <summary>
        /// Status of the step
        /// </summary>
        [JsonProperty("status")]
        public ChecklistItemStatusEnum Status
        {
            get => status;
            set { status = value; NotifyPropertyChanged(nameof(Status)); }
        }

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

    }


}
