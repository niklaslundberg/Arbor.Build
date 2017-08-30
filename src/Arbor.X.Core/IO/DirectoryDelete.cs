using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.IO
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
            _logger = logger ?? new NullLogger();
            _directoryFilters = directoryFilters.ToList();
            _fileFilters = fileFilters.ToList();

            WriteFilters();
        }

        private string DirectoryFilterList
        {
            get
            {
                if (!_directoryFilters.Any())
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
                if (!_fileFilters.Any())
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
                _logger.WriteVerbose(
                    $"Directory name '{baseDirectory.Name} is in filter list {filterList}, ignoring deleting directory");
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
                        _logger.WriteVerbose(
                            $"File name '{fileToDelete.Name} is in filter list {filterList}, ignoring deleting directory");
                        continue;
                    }

                    try
                    {
                        fileToDelete.Delete();
                        _logger.WriteVerbose($"Deleted file '{fileToDelete.FullName}'");
                    }
                    catch (IOException ex)
                    {
                        _logger.WriteError($"Could not delete file '{fileToDelete.FullName}', {ex}");
                    }
                }
            }
            else
            {
                _logger.WriteVerbose($"Delete self files is false, skipping deleting files in directory '{baseDir}'");
            }

            foreach (DirectoryInfo directoryToDelete in baseDirectory.EnumerateDirectories())
            {
                Delete(directoryToDelete.FullName);
            }

            if (!deleteSelf)
            {
                _logger.WriteVerbose($"Delete self is false, skipping deleting directory '{baseDir}'");
            }

            if (!baseDirectory.EnumerateFileSystemInfos().Any())
            {
                try
                {
                    baseDirectory.Delete();
                    _logger.WriteVerbose($"Deleted directory '{baseDirectory.FullName}'");
                }
                catch (IOException ex)
                {
                    _logger.WriteError($"Could not delete directory '{baseDirectory.FullName}', {ex}");
                }
            }
            else
            {
                _logger.WriteVerbose($"Directory '{baseDirectory.FullName}' still has files or directories");
            }
        }

        private void WriteFilters()
        {
            _logger.WriteVerbose($"Directory filters: {DirectoryFilterList}");
            _logger.WriteVerbose($"File filters: {FileFilterList}");
        }
    }
}
