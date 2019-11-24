using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using GooglePhotosSharp.Config;

namespace GooglePhotosSharp.LocalPhotos
{
    /// <summary>
    /// Represents a library of photos on disc. Each folder represents an album.
    /// </summary>
    public class LocalPhotoLibrary : ILocalPhotoLibrary
    {
        private readonly PhotoLibraryConfig _config;
        private readonly IFileSystem _fileSystem;

        private static readonly HashSet<string> AllowedFileTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".BMP", ".GIF", ".HEIC", ".ICO", ".JPG", ".JPEG", ".PNG", ".TIFF", ".WEBP"
        };

        public LocalPhotoLibrary(PhotoLibraryConfig config, IFileSystem fileSystem = null)
        {
            _config = config;
            _fileSystem = fileSystem ?? new FileSystem();
        }

        public IReadOnlyList<LocalPhoto> GetAllPhotos()
        {
            var allFiles = _fileSystem.Directory
                .GetFiles(_config.Path, "*", SearchOption.AllDirectories)
                // only include image files
                .Where(f => AllowedFileTypes.Contains(_fileSystem.Path.GetExtension(f)))
                // don't include files in the root of the library
                .Where(f => _fileSystem.Path.GetDirectoryName(f) != _config.Path)
                .AsEnumerable();

            if (_config.IncludeRegexes != null && _config.IncludeRegexes.Any())
            {
                allFiles = allFiles.Where(f => _config.IncludeRegexes.Any(r => Regex.IsMatch(f, r)));
            }

            if (_config.ExcludeRegexes != null && _config.ExcludeRegexes.Any())
            {
                allFiles = allFiles.Where(f => !_config.ExcludeRegexes.Any(r => Regex.IsMatch(f, r)));
            }

            return allFiles.Select(f => new LocalPhoto(_fileSystem)
                {
                    Path = f,
                    AlbumName = _fileSystem.Path.GetDirectoryName(f)
                        .Replace(_config.Path, "")
                        .Split(new [] { "/", "\\" }, StringSplitOptions.RemoveEmptyEntries)
                        .First()
                })
                .ToList();
        }
    }
}