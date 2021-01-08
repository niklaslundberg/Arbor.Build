using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog;
using Serilog.Core;
using Zio;

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
            ILogger? logger = null)
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

        public bool Delete(
            DirectoryEntry baseDirectory,
            bool deleteSelf = false,
            bool deleteSelfFiles = true)
        {
            bool deletedSelf = true;

            if (baseDirectory is null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            if (!baseDirectory.Exists)
            {
                return false;
            }

            if (_directoryFilters.Any(
                    filter => baseDirectory.Name.Equals(filter, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Verbose("Directory name '{Name} is in filter list {FilterList}, ignoring deleting directory",
                    baseDirectory.Name,
                    DirectoryFilterList);
                return false;
            }

            if (deleteSelfFiles)
            {
                foreach (var fileToDelete in baseDirectory.EnumerateFiles())
                {
                    if (_fileFilters.Any(
                            filter => fileToDelete.Name.Equals(filter, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.Verbose("File name '{Name} is in filter list {FilterList}, ignoring deleting directory",
                            fileToDelete.Name,
                            FileFilterList);
                        deletedSelf = false;
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
                    baseDirectory.Path.FullName);
            }

            bool allChildrenDeleted = true;

            foreach (var directoryToDelete in baseDirectory.EnumerateDirectories())
            {
               bool deleted = Delete(directoryToDelete, true);

               if (!deleted)
               {
                   allChildrenDeleted = false;
               }
            }

            if (!allChildrenDeleted)
            {
                return false;
            }

            if (!deleteSelf)
            {
                _logger.Verbose("Delete self is false, skipping deleting directory '{BaseDir}'", baseDirectory.Path.FullName);
                return false;
            }

            if (!baseDirectory.EnumerateFiles().Any())
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

            return deletedSelf;
        }

        private void WriteFilters()
        {
            _logger.Verbose("Directory filters: {DirectoryFilterList}", DirectoryFilterList);
            _logger.Verbose("File filters: {FileFilterList}", FileFilterList);
        }
    }
}
