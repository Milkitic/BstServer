using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace Milkitic.FileExplorer.Views
{
    public class FileCell : ViewCell
    {
        private const string DisableFlag = "._disabled";

        public FileCell(FileInfo fileInfo, ExplorerSettings settings) : base(fileInfo, settings)
        {
            File = fileInfo;
            Extension = fileInfo.Extension.Trim('.');
            FileSize = fileInfo.Length;
            FileSizeString = Explorer.ToSizeString(FileSize);
            var savePath = Path.Combine(Explorer.IconCachePath, Extension + ".png");
            if (!Explorer.IconCache.ContainsKey(Extension.ToLower()))
            {
                if (!System.IO.File.Exists(savePath) && System.IO.File.Exists(FullPath))
                {
                    Explorer.IconCache.Add(Extension, Icon.ExtractAssociatedIcon(FullPath).ToBitmap());

                    Explorer.IconCache[Extension].Save(savePath);
                }
            }

            if (System.IO.File.Exists(FullPath))
                FileIcon = Explorer.IconCache[Extension.ToLower()];
        }

        [JsonProperty("extension")]
        public string Extension { get; set; }
        [JsonProperty("file_size")]
        public long FileSize { get; set; }
        [JsonProperty("file_size_string")]
        public string FileSizeString { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName => Name.Replace("._disabled", "");

        [JsonProperty("is_enabled")]
        public bool IsEnabled
        {
            get => !File.Name.EndsWith(DisableFlag);
            set
            {
                bool enableFlag = value;
                if (IsEnabled && !enableFlag)
                    File.MoveTo(File.FullName + DisableFlag);
                else if (!IsEnabled && enableFlag)
                    File.MoveTo(File.FullName.Replace("._disabled", ""));
            }
        }

        [JsonIgnore]
        public Image FileIcon { get; set; }

        [JsonIgnore]
        public FileInfo File { get; }

        [JsonProperty("type")]
        public override string Type => Extension.ToLower() == "cfg" ? "cfg" : "file";
    }
}
