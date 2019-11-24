using System.Collections.Generic;

namespace GooglePhotosSharp.LocalPhotos
{
    public interface ILocalPhotoLibrary
    {
        IReadOnlyList<LocalPhoto> GetAllPhotos();
    }
}