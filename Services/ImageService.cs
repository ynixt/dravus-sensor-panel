using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace DravusSensorPanel.Services;

public class ImageInfo {
    public string Name { get; set; }
    public string Path { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ImageService {
    public ImageInfo CopyImage(string sourcePath, string destinationPath, string originalExt) {
        destinationPath = destinationPath.TrimEnd('/');

        (IImageEncoder, string) encoder = GetBestEncoder(originalExt);
        string destPath = Path.Combine(sourcePath, destinationPath + encoder.Item2);
        string fileName = Path.GetFileName(destPath);

        using ( Image image = Image.Load(sourcePath) ) {
            using ( FileStream fs = File.Open(destPath, FileMode.Create) ) {
                image.Mutate(x => x.AutoOrient());
                image.Metadata.ExifProfile = null;

                image.Save(fs, encoder.Item1);

                return new ImageInfo {
                    Name = fileName,
                    Path = destPath,
                    Width = image.Width,
                    Height = image.Height,
                };
            }
        }
    }

    private (IImageEncoder, string) GetBestEncoder(string originalExt) {
        if ( originalExt.Equals(".png", StringComparison.CurrentCultureIgnoreCase) ||
             originalExt.Equals(".webp", StringComparison.CurrentCultureIgnoreCase) ) {
            return ( new PngEncoder(), ".png" );
        }

        return ( new JpegEncoder(), ".jpg" );
    }
}
