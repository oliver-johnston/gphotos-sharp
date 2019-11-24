using System.Collections.Generic;

namespace GooglePhotosSharp.Config
{
    public class PhotoLibraryConfig
    {
        public string Email { get; set; }

        public string Path { get; set; }

        public IList<string> IncludeRegexes { get; set; }

        public IList<string> ExcludeRegexes { get; set; }
    }
}