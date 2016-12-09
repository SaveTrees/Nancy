namespace Nancy.ViewEngines
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Default implementation for retrieving information about views that are stored on the file system.
    /// </summary>
    public class DefaultFileSystemReader : IFileSystemReader
    {
        /// <summary>
        /// Gets information about view that are stored in folders below the applications root path.
        /// </summary>
        /// <param name="path">The path of the folder where the views should be looked for.</param>
        /// <param name="supportedViewExtensions">A list of view extensions to look for.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing view locations and contents readers.</returns>
        public IEnumerable<Tuple<string, Func<StreamReader>>> GetViewsWithSupportedExtensions(string path, IEnumerable<string> supportedViewExtensions)
        {
            return GetFilenames(path, supportedViewExtensions, "*.*", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Gets information about specific views that are stored in folders below the applications root path.
        /// </summary>
        /// <param name="path">The path of the folder where the views should be looked for.</param>
        /// <param name="viewName">Name of the view to search for</param>
        /// <param name="supportedViewExtensions">A list of view extensions to look for.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing view locations and contents readers.</returns>
        public IEnumerable<Tuple<string, Func<StreamReader>>> GetViewsWithSupportedExtensions(string path, string viewName, IEnumerable<string> supportedViewExtensions)
        {
            return GetFilenames(path, supportedViewExtensions, viewName + ".*", SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Gets the last modified time for the file specified
        /// </summary>
        /// <param name="filename">Filename</param>
        /// <returns>Time the file was last modified</returns>
        public DateTime GetLastModified(string filename)
        {
            return File.GetLastWriteTimeUtc(filename);
        }

        private static IEnumerable<Tuple<string, Func<StreamReader>>> GetFilenames(string path, IEnumerable<string> supportedViewExtensions, string searchPattern, SearchOption searchOption, Func<FileInfo, bool> filter = null)
        {
            return Directory.GetFiles(path, searchPattern, searchOption)
                .Where(fileName => !fileName.Substring(path.Length + 1).StartsWith("obj"))
                .Where(f => IsValidExtention(f, supportedViewExtensions))
                .Distinct()
                .Select(file => new Tuple<string, Func<StreamReader>>(file, () => new StreamReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))));
        }

        private static bool IsValidExtention(string filename, IEnumerable<string> supportedViewExtensions)
        {
            var extension = Path.GetExtension(filename);
            var isValidExtention = !string.IsNullOrEmpty(extension) && supportedViewExtensions.Contains(extension.Substring(1));

            return isValidExtention;
        }
    }
}