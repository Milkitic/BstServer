namespace Milkitic.FileExplorer
{
    public class ExplorerSettings
    {
        public uint CountsPerPage { get; set; } = 0;
        public bool ShowHidden { get; set; } = false;
        public bool UseHighestPermission { get; set; }
    }
}
