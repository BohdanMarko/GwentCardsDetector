using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GwentCardsDetector;

public sealed class DeckResolver
{
    const string TemplatesPath = @".\templates";

    public static string ResolveDeckName(Image<Rgba32> inputCard)
    {
        string[] templates = Directory.GetFiles(TemplatesPath);
        Dictionary<string, double> similarities = [];

        inputCard.Mutate(x => x.AutoOrient());
        var newInputCard = CropImage(inputCard);

        foreach (string template in templates)
        {
            using Image<Rgba32> templateImage = Image.Load<Rgba32>(template);

            var newTemplateImage = CropImage(templateImage);
            newInputCard.Mutate(ctx => ctx.Resize(newTemplateImage.Size));

            newTemplateImage.Save(@"E:\source\projects\GwentCardsDetector\template.jpg");
            newInputCard.Save(@"E:\source\projects\GwentCardsDetector\input.jpg");

            double similarity = CalculatePixelSimilarity(newInputCard, newTemplateImage);
            string templateName = Path.GetFileNameWithoutExtension(template);
            similarities.Add(templateName, similarity);
        }

        return MapTemplateNameToDeckName(similarities.MaxBy(x => x.Value).Key);
    }

    /// <summary>
    /// Crops the specified image by the given number of pixels from each side.
    /// </summary>
    /// <param name="image">The original image to crop.</param>
    /// <param name="cropTopBottom">Pixels to crop from the top and bottom.</param>
    /// <param name="cropLeftRight">Pixels to crop from the left and right.</param>
    /// <returns>A new cropped image.</returns>
    static Image<Rgba32> CropImage(Image<Rgba32> image, int cropTopBottom = 50, int cropLeftRight = 30)
    {
        // Ensure the crop amount is valid (not larger than the image dimensions)
        if (cropTopBottom * 2 >= image.Height || cropLeftRight * 2 >= image.Width)
        {
            throw new ArgumentException("Crop amount is too large for the image size.");
        }

        // Define the crop rectangle
        var cropRectangle = new Rectangle(
            cropLeftRight,
            cropTopBottom,
            image.Width - cropLeftRight * 2,
            image.Height - cropTopBottom * 2
        );

        // Perform the crop and return a new cropped image
        return image.Clone(ctx => ctx.Crop(cropRectangle));
    }

    static string MapTemplateNameToDeckName(string templateName)
        => templateName switch
        {
            "monsters-card" => "Монстри",
            "nilfgaard-card" => "Нiльфгаард",
            "redaniya-card" => "Реданiя",
            "scoiatael-card" => "Скольятели",
            "skellige-card" => "Скелiге",
            "temeriya-card" => "Темерiя",
            "toussaint-card" => "Туссант",
            "velen-card" => "Велен",
            "wild_hunt-card" => "Дика Охота",
            "witchers-card" => "Вiдьмаки",
            _ => "Невiдомо"
        };

    static double CalculatePixelSimilarity(Image<Rgba32> croppedInputCard, Image<Rgba32> croppedTemplate)
    {
        int matchingPixels = 0;
        int totalPixels = croppedInputCard.Width * croppedInputCard.Height;

        for (int y = 0; y < croppedInputCard.Height; y++)
        {
            for (int x = 0; x < croppedInputCard.Width; x++)
            {
                if (ArePixelsSimilar(croppedInputCard[x, y], croppedTemplate[x, y]))
                {
                    matchingPixels++;
                }
            }
        }

        return (double)matchingPixels / totalPixels;
    }

    static bool ArePixelsSimilar(Rgba32 pixel1, Rgba32 pixel2, int tolerance = 10)
        => Math.Abs(pixel1.R - pixel2.R) <= tolerance
        && Math.Abs(pixel1.G - pixel2.G) <= tolerance
        && Math.Abs(pixel1.B - pixel2.B) <= tolerance;

}
