﻿namespace LMSRepository.Models
{
    public class AssetPhoto : Photo
    {
        public LibraryAsset LibraryAsset { get; set; }
        public int LibraryAssetId { get; set; }
    }
}