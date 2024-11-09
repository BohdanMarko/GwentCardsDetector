using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;

namespace GwentCardsDetector;

static class SingleCardDetector
{
    const string InputImagePath = @"E:\source\projects\GwentCardsDetector\input\{0}";
    const string TemplatesPath = @".\templates";
    const string ResultImagePath = @"E:\source\projects\GwentCardsDetector\result.jpg";

    public static void Detect(string inputImageName)
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

        inputImage.Save(@"E:\source\projects\GwentCardsDetector\one.jpg");

        Rectangle cardRectangle = FindCardEdges(inputImage);

        if (cardRectangle != Rectangle.Empty)
        {
            using Image<Rgba32> originalImage = Image.Load<Rgba32>(inputImagePath);
            using Image<Rgba32> croppedImage = CropImage(originalImage, cardRectangle);

            string deckName = DeckResolver.ResolveDeckName(croppedImage);
            Console.WriteLine($"Карта належить до колоди: " + deckName);

            using Image<Rgba32> finalImage = Image.Load<Rgba32>(inputImagePath);
            finalImage.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Red, 6), cardRectangle));
            finalImage.Save(ResultImagePath);

            Process.Start(new ProcessStartInfo(ResultImagePath) { UseShellExecute = true });
        }
        else
        {
            Console.WriteLine("Карта не знайдена");
        }
    }

    static Image<Rgba32> CropImage(Image<Rgba32> image, Rectangle cardRectangle) => image.Clone(ctx => ctx.Crop(cardRectangle));

    static Rectangle FindCardEdges(Image<Rgba32> image)
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

    static bool IsBlack(Rgba32 pixel) => pixel.R == 0 && pixel.G == 0 && pixel.B == 0;
}
