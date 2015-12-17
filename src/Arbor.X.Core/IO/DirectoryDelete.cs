using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Arbor.X.Core.Logging;

namespace Arbor.X.Core.IO
{
    public class DirectoryDelete
    {
        readonly List<string> _directoryFilters;
        readonly List<string> _fileFilters;
        readonly ILogger _logger;

        public DirectoryDelete(IEnumerable<string> directoryFilters, IEnumerable<string> fileFilters,
            ILogger logger = null)
        {
            _logger = logger ?? new NullLogger();
            _directoryFilters = directoryFilters.ToList();
            _fileFilters = fileFilters.ToList();

            WriteFilters();
        }

        string DirectoryFilterList
        {
            get
            {
                if (!_directoryFilters.Any())
                {
                    return "No directory filters";
                }
                return string.Join(", ", _directoryFilters.Select(filter => string.Format("'{0}'", filter)));
            }
        }

        string FileFilterList
        {
            get
            {
                if (!_fileFilters.Any())
                {
                    return "No files filters";
                }
                return string.Join(", ", _fileFilters.Select(filter => string.Format("'{0}'", filter)));
            }
        }

        void WriteFilters()
        {
            _logger.WriteVerbose(string.Format("Directory filters: {0}", DirectoryFilterList));
            _logger.WriteVerbose(string.Format("File filters: {0}", FileFilterList));
        }

        public void Delete(string baseDir, bool deleteSelf = false, bool deleteSelfFiles = true)
        {
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                throw new ArgumentNullException("baseDir");
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
                    string.Format("Directory name '{0} is in filter list {1}, ignoring deleting directory",
                        baseDirectory.Name, filterList));
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
                            string.Format("File name '{0} is in filter list {1}, ignoring deleting directory",
                                fileToDelete.Name, filterList));
                        continue;
                    }

                    try
                    {
                        fileToDelete.Delete();
                        _logger.WriteVerbose(string.Format("Deleted file '{0}'", fileToDelete.FullName));
                    }
                    catch (IOException ex)
                    {
                        _logger.WriteError(string.Format("Could not delete file '{0}', {1}", fileToDelete.FullName, ex));
                    }
                }
            }
            else
            {
                _logger.WriteVerbose(string.Format("Delete self files is false, skipping deleting files in directory '{0}'", baseDir));
            }

            foreach (DirectoryInfo directoryToDelete in baseDirectory.EnumerateDirectories())
            {
                Delete(directoryToDelete.FullName);
            }

            if (!deleteSelf)
            {
                _logger.WriteVerbose(string.Format("Delete self is false, skipping deleting directory '{0}'", baseDir));
            }

            if (!baseDirectory.EnumerateFileSystemInfos().Any())
            {
                try
                {
                    baseDirectory.Delete();
                    _logger.WriteVerbose(string.Format("Deleted directory '{0}'", baseDirectory.FullName));
                }
                catch (IOException ex)
                {
                    _logger.WriteError(string.Format("Could not delete directory '{0}', {1}", baseDirectory.FullName, ex));
                }
            }
            else
            {
                _logger.WriteVerbose(string.Format("Directory '{0}' still has files or directories",
                    baseDirectory.FullName));
            }
        }
    }
}
