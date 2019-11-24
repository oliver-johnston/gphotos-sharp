using System.Collections.Generic;

namespace GooglePhotosSharp.Database
{
    public interface IDatabaseRepository
    {
        IList<Photo> GetPhotos();

        void AddOrUpdate(Photo photo);

    }
}