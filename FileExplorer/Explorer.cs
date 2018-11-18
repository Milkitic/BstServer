using Milkitic.FileExplorer.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Milkitic.FileExplorer
{
    public class Explorer
    {
        private readonly string _baseDirectory;

        private static string _iconCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon_cache");
        private bool _includeHidden;
        private uint _countsPerPage;

        static Explorer()
        {
            var files = new DirectoryInfo(IconCachePath).EnumerateFiles();
            foreach (var file in files)
            {
                IconCache.Add(file.Name.Split('.')[0], Image.FromFile(file.FullName));
            }
        }

        public static string ToSizeString(long @byte)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            const double modValue = 1024.0;
            double byteD = @byte;
            int j = 0;
            while (byteD >= modValue)
            {
                byteD /= modValue;
                j++;
            }
            return Math.Round(byteD, 2) + units[j];
        }

        public Explorer(string rootPath) : this(rootPath, "exchange", @"left4dead2\addons", new ExplorerSettings())
        {
        }

        public Explorer(string rootPath, string exchangePath, string addonPath, ExplorerSettings settings)
        {
            Settings = settings;
            _includeHidden = settings.ShowHidden;
            _countsPerPage = settings.CountsPerPage;
            ExchangePath = new DirectoryInfo(Path.Combine(rootPath, exchangePath));
            if (!ExchangePath.Exists)
                ExchangePath.Create();
            HighestPermission = settings.UseHighestPermission;
            _baseDirectory = new DirectoryInfo(rootPath).FullName;
            SetAddonPath(addonPath);
            NavigateHomeFolder();
        }

        public void Refresh()
        {
            ViewCells?.Clear();
            ViewCells = new List<ViewCell>();
            foreach (var directory in CurrentPath.EnumerateDirectories())
            {
                var cell = new DirectoryCell(directory, Settings);

                ViewCells.Add(cell);
            }

            foreach (var file in CurrentPath.EnumerateFiles())
            {
                if (DisplayFilter != null && !DisplayFilter.Contains(file.Extension.ToLower().Trim('.')))
                    continue;
                var cell = new FileCell(file, Settings);
                if (IsExchangePath || CurrentPath.FullName == AddonPath.FullName) cell.CanDelete = true;
                else cell.CanDelete = false;
                ViewCells.Add(cell);
            }
        }

        public IEnumerable<ViewCell> GetViewCells()
        {
            return GetViewCells(0);
        }

        public IEnumerable<ViewCell> GetViewCells(int page)
        {
            var baseCell = ViewCells.Select(k => k);
            if (CountsPerPage != 0)
                baseCell = baseCell.Skip(page * (int)CountsPerPage).Take((int)CountsPerPage);
            return IncludeHidden ? baseCell : baseCell.Where(k => !k.IsHidden);
        }

        public void NavigateHomeFolder()
        {
            CurrentPath = new DirectoryInfo(_baseDirectory);
            Refresh();
        }

        public void NavigateRelativeFolder(string path)
        {
            string absolutePath;
            if (path.StartsWith("~") || path.StartsWith("/") || path.StartsWith("\\"))
            {
                absolutePath = Path.Combine(_baseDirectory, path.Trim('~', '/', '\\'));
            }
            else
            {
                absolutePath = Path.Combine(CurrentPath.FullName, path);
            }

            var di = new DirectoryInfo(absolutePath);
            CheckFolderAccess(di);
            CurrentPath = di;
            Refresh();
        }

        public void NavigateToUpload()
        {
            CheckFolderAccess(ExchangePath);
            CurrentPath = ExchangePath;
            Refresh();
        }

        public void NavigateParentFolder()
        {
            Refresh();
            CheckFolderAccess(CurrentPath.Parent);
            CurrentPath = CurrentPath.Parent;
            Refresh();
        }

        public void NavigateSubFolder(string childName)
        {
            Refresh();
            CheckSubFolderAccess(childName);
            var dirs = ViewCells.Where(k => k is DirectoryCell);
            if (!IncludeHidden) dirs = dirs.Where(k => !k.IsHidden);
            CurrentPath = ((DirectoryCell)dirs.First(k => k.Name == childName)).Directory;
            Refresh();
        }

        public void NavigateSubFolder(Guid childGuid)
        {
            Refresh();
            var child = ViewCells.FirstOrDefault(k => k.Guid == childGuid);
            CheckSubFolderAccess(child?.Name);
            var dirs = ViewCells.Where(k => k is DirectoryCell);
            if (!IncludeHidden) dirs = dirs.Where(k => !k.IsHidden);
            CurrentPath = ((DirectoryCell)dirs.First(k => k.Guid == childGuid)).Directory;
            Refresh();
        }

        public void EnableFile(string childName)
        {
            FileCell file = SearchChildFile(childName);

            if (!file.IsEnabled)
                file.IsEnabled = true;
            else
                Console.WriteLine(childName + " is already enabled!");
            Refresh();
        }

        public void DisableFile(string childName)
        {
            FileCell file = SearchChildFile(childName);

            if (file.IsEnabled)
                file.IsEnabled = false;
            else
                Console.WriteLine(childName + " is already disabled!");
            Refresh();
        }

        public void DeleteFile(string childName)
        {
            FileCell file = SearchChildFile(childName);
            if (file.CanDelete)
                File.Delete(file.FullPath);
            else
                throw new UnauthorizedAccessException("You cannot delete this file.");
        }

        public void SetAddonPath(string relativePath)
        {
            var di = new DirectoryInfo(Path.Combine(_baseDirectory, relativePath));
            if (!di.Exists)
                di.Create();
            AddonPath = di;
        }

        public void SaveContent(string childName, string content)
        {
            FileCell file = SearchChildFile(childName);
            if (!TryGetContent(file.FullPath, out _))
                throw new InvalidOperationException("Can't overwrite binary file.");
            if (content.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t'))
                throw new InvalidOperationException("Invalid char.");
            File.WriteAllText(file.FullPath, content);
            Refresh();
        }


        public FileCell SearchChildFile(string childName)
        {
            var filteredList = ViewCells.Where(path => path is FileCell);
            if (!IncludeHidden) filteredList = filteredList.Where(k => !k.IsHidden);
            var file = (FileCell)filteredList.FirstOrDefault(k => k.Name == childName);
            if (file == null)
            {
                throw new FileNotFoundException("No such file or no access to the file. File name: " + childName);
            }

            return file;
        }


        public override string ToString()
        {
            return $"{{{Guid}}} {base.ToString()}";
        }

        public static bool TryGetContent(string path, out string content)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        if (line.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t'))
                        {
                            content = "** BINARY FILE **";
                            return false;
                        }

                        sb.AppendLine(line);
                        line = sr.ReadLine();
                    }
                }
                content = sb.ToString();
                return true;
            }
            finally
            {
                sb.Clear();
            }
        }

        public Guid Guid { get; } = Guid.NewGuid();
        public ExplorerSettings Settings { get; }
        public List<ViewCell> ViewCells { get; private set; }
        public DirectoryInfo CurrentPath { get; private set; }
        public DirectoryInfo AddonPath { get; private set; }
        public DirectoryInfo ExchangePath { get; }
        public string CurrentPathText => "~" + CurrentPath.FullName.Substring(_baseDirectory.Length);
        public bool IsExchangePath => ExchangePath.FullName == CurrentPath.FullName;
        public bool IncludeHidden
        {
            get => _includeHidden;
            set
            {
                _includeHidden = value;
                Refresh();
            }
        }
        public string[] DisplayFilter { get; set; }
        public bool HighestPermission { get; set; }
        public uint CountsPerPage
        {
            get => _countsPerPage;
            set
            {
                _countsPerPage = value;
                Refresh();
            }
        }
        public int Pages => CountsPerPage == 0 ? 1 : ViewCells.Count / (int)CountsPerPage + 1;
        public static Dictionary<string, Image> IconCache { get; set; } = new Dictionary<string, Image>();
        public static string IconCachePath
        {
            get
            {
                if (!Directory.Exists(_iconCachePath))
                    Directory.CreateDirectory(_iconCachePath);
                return _iconCachePath;
            }
            set => _iconCachePath = value;
        }


        private void CheckSubFolderAccess(string childName)
        {
            var filteredList = ViewCells.Where(path => path is DirectoryCell);
            if (!IncludeHidden) filteredList = filteredList.Where(k => !k.IsHidden);
            var dir = filteredList.FirstOrDefault(k => k.Name == childName);
            if (dir == null)
            {
                throw new DirectoryNotFoundException("No such directory or no access to the directory. Directory name: " + childName);
            }

            var di = ((DirectoryCell)dir).Directory;
            foreach (var _ in di.EnumerateFiles()) break;
            foreach (var _ in di.EnumerateDirectories()) break;
        }

        private void CheckFolderAccess(DirectoryInfo directory)
        {
            if (directory == null)
                throw new DirectoryNotFoundException("No such directory or no access to the directory.");
            if (!directory.Exists)
                throw new DirectoryNotFoundException("No such directory or no access to the directory. Directory name: " + directory.Name);
            if (!directory.FullName.Contains(_baseDirectory))
                throw new UnauthorizedAccessException(@"Access to the base path is denied.");
        }

    }
}
