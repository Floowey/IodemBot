using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IodemBot.Core.UserManagement;

public class ImageStitcher
{
    public static void GenerateCompass(UserAccount user)
    {
        var oaths = new[] { Oath.Jupiter, Oath.Mercury, Oath.Mars, Oath.Venus };
        var completions = oaths.Select(o => (int)user.Oaths.GetOathCompletion(o)).ToArray();

        Image<Rgba32> stitchedImage = ImageStitcher.GetCompass(completions);
        stitchedImage.Save("compass.png");
    }

    public static Image<Rgba32> GetCompass(int[] completions)
    {
        var prefix = "Resources/Images/Compass";
        var paths = new[] { "sprite_05", "sprite_00", "sprite_06", "sprite_01", "sprite_04", "sprite_03", "sprite_07", "sprite_02", "sprite_08" };

        var upgrades = new[] { "sprite_05", "sprite_09", "sprite_06", "sprite_10", "sprite_04", "sprite_12", "sprite_07", "sprite_11", "sprite_08" };

        var imagePaths = paths.Select(p => $"{prefix}/{p}.png").ToArray();
        var upgradePaths = upgrades.Select(p => $"{prefix}/{p}.png").ToArray();

        Image<Rgba32>[] images = new Image<Rgba32>[imagePaths.Length];

        var completions_ = new[] { Math.Max(completions[0], completions[1]), completions[0], Math.Max(completions[0], completions[2]),
            completions[1], new[]{completions[0], completions[1], completions[2], completions[3] }.Max() ,completions[2],
            Math.Max(completions[1], completions[3]), completions[3], Math.Max(completions[2], completions[3])
        };

        for (int i = 0; i < imagePaths.Length; i++)
        {
            var img = Image.Load<Rgba32>(imagePaths[i]);
            var completed = completions_[i];

            if (completed == 0)
            {
                img.Mutate(x => x.Grayscale());
            }
            else if (completed == 2)
            {
                img = Image.Load<Rgba32>(upgradePaths[i]);
            }

            images[i] = img;
        }

        // Call the StitchImages method with the loaded images
        return StitchImages(images, 3, 3);
    }

    public static Image<Rgba32> StitchImages(string[] imagePaths, int numRows, int numCols)
    {
        Image<Rgba32>[] images = new Image<Rgba32>[imagePaths.Length];

        for (int i = 0; i < imagePaths.Length; i++)
        {
            var img = Image.Load<Rgba32>(imagePaths[i]);
            img.Mutate(x => x.Grayscale());
            images[i] = img;
        }

        // Call the StitchImages method with the loaded images
        return StitchImages(images, numRows, numCols);
    }

    public static Image<Rgba32> StitchImages(Image<Rgba32>[] images, int numRows, int numCols)
    {
        int stitchedImageWidth = images[0].Width * numCols; // Calculate the width of the stitched image
        int stitchedImageHeight = images[0].Height * numRows; // Calculate the height of the stitched image

        using (var stitchedImage = new Image<Rgba32>(stitchedImageWidth, stitchedImageHeight)) // Create a new image for the stitched image
        {
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numCols; col++)
                {
                    int imageIndex = row * numCols + col; // Calculate the index of the current image in the images array

                    if (imageIndex < images.Length)
                    {
                        // Calculate the position of the current image in the stitched image
                        int xPos = col * images[0].Width;
                        int yPos = row * images[0].Height;

                        // Draw the current image at the calculated position
                        stitchedImage.Mutate(x => x.DrawImage(images[imageIndex], new Point(xPos, yPos), 1f));
                    }
                }
            }

            return stitchedImage.Clone(); // Return a clone of the stitched image to release resources
        }
    }
}