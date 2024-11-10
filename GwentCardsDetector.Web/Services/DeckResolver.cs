using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GwentCardsDetector.Web.Services;

public sealed class DeckResolver
{
    const string TemplatesPath = @"wwwroot/templates";

    public string ResolveDeckName(Image<Rgba32> inputCard)
    {
        string[] templates = Directory.GetFiles(TemplatesPath);
        Dictionary<string, double> similarities = [];

        inputCard.Mutate(ctx => ctx.AutoOrient().Crop(GetCropSettings(inputCard)));
        Image<Rgba32> preparedInputCard = AdjustBrightness(inputCard);

        foreach (string template in templates)
        {
            using Image<Rgba32> templateImage = Image.Load<Rgba32>(template);
            templateImage.Mutate(ctx => ctx.Crop(GetCropSettings(templateImage)));
            
            Image<Rgba32> preparedTemplateImage = AdjustBrightness(templateImage);
            preparedInputCard.Mutate(ctx => ctx.Resize(preparedTemplateImage.Size));
            
            double similarity = CalculatePixelSimilarity(preparedInputCard, preparedTemplateImage);
            string templateName = Path.GetFileNameWithoutExtension(template);
            
            similarities.Add(templateName, similarity);
        }

        return MapTemplateNameToDeckName(similarities.MaxBy(x => x.Value).Key);
    }

    static Image<Rgba32> AdjustBrightness(Image<Rgba32> image)
    {
        float avgBrightness = CalculateAverageBrightness(image);
        // Target brightness level (can be tuned as needed)
        const float targetBrightness = 0.1f;
        float adjustmentFactor = targetBrightness / avgBrightness;
        image.Mutate(ctx => ctx.Brightness(adjustmentFactor));
        return image;
    }

    static float CalculateAverageBrightness(Image<Rgba32> image)
    {
        float totalBrightness = 0;
        int totalPixels = image.Width * image.Height;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                float brightness = (pixel.R + pixel.G + pixel.B) / (3 * 255f);
                totalBrightness += brightness;
            }
        }

        return totalBrightness / totalPixels;
    }

    static Rectangle GetCropSettings(Image<Rgba32> image, int cropTopBottom = 50, int cropLeftRight = 30)
        => new(cropLeftRight, cropTopBottom,
            image.Width - cropLeftRight * 2,
            image.Height - cropTopBottom * 2);

    static string MapTemplateNameToDeckName(string templateName) => templateName switch
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
        _ => "Невiдома колода"
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

    static bool ArePixelsSimilar(Rgba32 pixel1, Rgba32 pixel2, int tolerance = 15)
        => Math.Abs(pixel1.R - pixel2.R) <= tolerance 
        && Math.Abs(pixel1.G - pixel2.G) <= tolerance
        && Math.Abs(pixel1.B - pixel2.B) <= tolerance;
}
