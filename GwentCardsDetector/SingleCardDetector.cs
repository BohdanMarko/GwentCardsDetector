using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;

namespace GwentCardsDetector;

sealed class SingleCardDetector
{
    const string InputImagePath = @".\input\{0}.jpg";
    const string TemplatesPath = @".\templates";
    const string ResultImagePath = @".\result.jpg";

    public void Detect(string inputImageName)
    {
        string inputImagePath = string.Format(InputImagePath, inputImageName);
        using Image<Rgba32> inputImage = Image.Load<Rgba32>(inputImagePath);
        // Попередня обробка зображення:
        //      розмиття для зменшення шуму,
        //      покращення контрасту,
        //      перетворення в градації сірого,
        //      порогова фільтрація для виділення чорного контуру карти
        inputImage.Mutate(
            ctx => ctx
                .GaussianBlur(1.5f)
                .Contrast(1.5f)
                .Brightness(1.1f)
                .Grayscale()
                .BinaryThreshold(0.35f));

        //inputImage.Save(@"E:\projects\GwentCardsDetector\processed.jpg");

        Rectangle cardRectangle = FindCardEdges(inputImage);

        if (cardRectangle != Rectangle.Empty)
        {
            using Image<Rgba32> originalImage = Image.Load<Rgba32>(inputImagePath);
            using Image<Rgba32> croppedImage = CropImage(originalImage, cardRectangle);
            //croppedImage.Save(@"E:\projects\GwentCardsDetector\cropped.jpg");

            string deckName = ResolveDeckName(croppedImage);
            Console.WriteLine($"Карта належить до колоди: " + deckName);

            using Image<Rgba32> finalImage = Image.Load<Rgba32>(inputImagePath);
            finalImage.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Red, 6), cardRectangle));
            finalImage.Save(ResultImagePath);

            System.Diagnostics.Process.Start(new ProcessStartInfo(ResultImagePath) { UseShellExecute = true });
        }
        else
        {
            Console.WriteLine("Карта не знайдена");
        }
    }

    string ResolveDeckName(Image<Rgba32> inputCard)
    {
        string[] templates = Directory.GetFiles(TemplatesPath);
        Dictionary<string, double> similarities = [];

        inputCard.Mutate(x => x.AutoOrient().BlackWhite());

        foreach (string template in templates)
        {
            using Image<Rgba32> templateImage = Image.Load<Rgba32>(template);
            templateImage.Mutate(x => x.BlackWhite());
            inputCard.Mutate(ctx => ctx.Resize(templateImage.Size));
            //templateImage.Save(@$"E:\source\projects\GwentCardsDetector\template.jpg");
            //inputCard.Save(@$"E:\source\projects\GwentCardsDetector\input.jpg");
            double similarity = CalculatePixelSimilarity(inputCard, templateImage);
            string templateName = Path.GetFileNameWithoutExtension(template);
            similarities.Add(templateName, similarity);
        }

        return MapTemplateNameToDeckName(similarities.MaxBy(x => x.Value).Key);
    }

    string MapTemplateNameToDeckName(string templateName)
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

    double CalculatePixelSimilarity(Image<Rgba32> croppedInputCard, Image<Rgba32> croppedTemplate)
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

    bool ArePixelsSimilar(Rgba32 pixel1, Rgba32 pixel2, int tolerance = 10)
        => Math.Abs(pixel1.R - pixel2.R) <= tolerance
        && Math.Abs(pixel1.G - pixel2.G) <= tolerance
        && Math.Abs(pixel1.B - pixel2.B) <= tolerance;

    Image<Rgba32> CropImage(Image<Rgba32> image, Rectangle cardRectangle) => image.Clone(ctx => ctx.Crop(cardRectangle));

    Rectangle FindCardEdges(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;

        int left = width, right = 0, top = height, bottom = 0;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    if (IsBlack(row[x]))
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }
        });

        if (left < right && top < bottom)
        {
            return new Rectangle(left, top, right - left, bottom - top);
        }

        return Rectangle.Empty;
    }

    bool IsBlack(Rgba32 pixel) => pixel.R == 0 && pixel.G == 0 && pixel.B == 0;
}
