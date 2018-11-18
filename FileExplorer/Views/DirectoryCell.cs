using System.IO;
using Newtonsoft.Json;

namespace Milkitic.FileExplorer.Views
{
    public class DirectoryCell : ViewCell
    {
        public DirectoryCell(DirectoryInfo directory, ExplorerSettings settings) : base(directory, settings)
        {
            Directory = directory;
        }

        [JsonIgnore]
        public DirectoryInfo Directory { get; }

        [JsonProperty("type")]
        public override string Type => "dir";
    }
}
