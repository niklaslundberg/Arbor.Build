using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.IO
{
    public class DirectoryDelete
    {
        private readonly List<string> _directoryFilters;
        private readonly List<string> _fileFilters;
        private readonly ILogger _logger;

        public DirectoryDelete(
            IEnumerable<string> directoryFilters,
            IEnumerable<string> fileFilters,
            ILogger logger = null)
        {
            _logger = logger ?? Logger.None;
            _directoryFilters = directoryFilters.ToList();
            _fileFilters = fileFilters.ToList();

            WriteFilters();
        }

        private string DirectoryFilterList
        {
            get
            {
                if (_directoryFilters.Count == 0)
                {
                    return "No directory filters";
                }

                return string.Join(", ", _directoryFilters.Select(filter => $"'{filter}'"));
            }
        }

        private string FileFilterList
        {
            get
            {
                if (_fileFilters.Count == 0)
                {
                    return "No files filters";
                }

                return string.Join(", ", _fileFilters.Select(filter => $"'{filter}'"));
            }
        }

        public void Delete(
            string baseDir,
            bool deleteSelf = false,
            bool deleteSelfFiles = true)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                throw new ArgumentNullException(nameof(baseDir));
            }

            var baseDirectory = new DirectoryInfo(baseDir);

            if (!baseDirectory.Exists)
            {
                return;
            }

            if (
                _directoryFilters.Any(
                    filter => baseDirectory.Name.Equals(filter, StringComparison.InvariantCultureIgnoreCase)))
            {
                string filterList = DirectoryFilterList;
                _logger.Verbose("Directory name '{Name} is in filter list {FilterList}, ignoring deleting directory",
                    baseDirectory.Name,
                    filterList);
                return;
            }

            if (deleteSelfFiles)
            {
                foreach (FileInfo fileToDelete in baseDirectory.EnumerateFiles())
                {
                    if (
                        _fileFilters.Any(
                            filter => fileToDelete.Name.Equals(filter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        string filterList = FileFilterList;
                        _logger.Verbose("File name '{Name} is in filter list {FilterList}, ignoring deleting directory",
                            fileToDelete.Name,
                            filterList);
                        continue;
                    }

                    try
                    {
                        fileToDelete.Delete();
                        _logger.Verbose("Deleted file '{FullName}'", fileToDelete.FullName);
                    }
                    catch (IOException ex)
                    {
                        _logger.Error(ex, "Could not delete file '{FullName}'", fileToDelete.FullName);
                    }
                }
            }
            else
            {
                _logger.Verbose("Delete self files is false, skipping deleting files in directory '{BaseDir}'",
                    baseDir);
            }

            foreach (DirectoryInfo directoryToDelete in baseDirectory.EnumerateDirectories())
            {
                Delete(directoryToDelete.FullName);
            }

            if (!deleteSelf)
            {
                _logger.Verbose("Delete self is false, skipping deleting directory '{BaseDir}'", baseDir);
            }

            if (!baseDirectory.EnumerateFileSystemInfos().Any())
            {
                try
                {
                    baseDirectory.Delete();
                    _logger.Verbose("Deleted directory '{FullName}'", baseDirectory.FullName);
                }
                catch (IOException ex)
                {
                    _logger.Error(ex, "Could not delete directory '{FullName}'", baseDirectory.FullName);
                }
            }
            else
            {
                _logger.Verbose("Directory '{FullName}' still has files or directories", baseDirectory.FullName);
            }
        }

        private void WriteFilters()
        {
            _logger.Verbose("Directory filters: {DirectoryFilterList}", DirectoryFilterList);
            _logger.Verbose("File filters: {FileFilterList}", FileFilterList);
        }
    }
}
