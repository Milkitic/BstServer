using Newtonsoft.Json;
using System;
using System.IO;

namespace Milkitic.FileExplorer.Views
{
    public abstract class ViewCell
    {
        private readonly ExplorerSettings _settings;

        public ViewCell(FileSystemInfo fsi, ExplorerSettings settings)
        {
            _settings = settings;
            FullPath = fsi.FullName;
            Name = fsi.Name;
            LastWriteTime = fsi.LastWriteTime;
            CreationTime = fsi.CreationTime;
            Attributes = fsi.Attributes;
            CanDelete = Name.ToLower().EndsWith(".vpk") || _settings.UseHighestPermission;
        }

        [JsonProperty("guid")]
        public Guid Guid { get; set; } = Guid.NewGuid();
        [JsonProperty("full_path")]
        public string FullPath { get; set; }
        [JsonProperty("type")]
        public virtual string Type { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonIgnore]
        public DateTime LastWriteTime { get; set; }
        [JsonProperty("last_write")]
        public string LastWrite => LastWriteTime.ToString("g");
        [JsonIgnore]
        public DateTime CreationTime { get; set; }

        [JsonProperty("create_at")]
        public string CreationAt => CreationTime.ToString("g");
        [JsonIgnore]
        public FileAttributes Attributes { get; set; }

        [JsonIgnore]
        public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);
        [JsonIgnore]
        public bool IsSystem => Attributes.HasFlag(FileAttributes.System);
        [JsonIgnore]
        public bool IsReadOnly => Attributes.HasFlag(FileAttributes.ReadOnly);

        [JsonProperty("can_delete")]
        public bool CanDelete { get; set; }
    }
}
