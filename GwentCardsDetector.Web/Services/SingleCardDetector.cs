using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace GwentCardsDetector.Web.Services;

public class DetectionResult
{
    public string DetectedDeck { get; set; }
    public string HighlightedImagePath { get; set; }
    public string Message { get; set; }
}

public static class SingleCardDetector
{
    private const string TemplatesPath = "wwwroot/templates";
    private const string UploadsPath = "wwwroot/uploads";

    public static DetectionResult Detect(IFormFile uploadedFile)
    {
        if (uploadedFile == null || uploadedFile.Length == 0)
            return null;

        Directory.CreateDirectory(UploadsPath);

        var fileName = Path.GetRandomFileName() + Path.GetExtension(uploadedFile.FileName);
        var inputImagePath = Path.Combine(UploadsPath, fileName);

        using (var stream = new FileStream(inputImagePath, FileMode.Create))
        {
            uploadedFile.CopyTo(stream);
        }

        using Image<Rgba32> inputImage = Image.Load<Rgba32>(inputImagePath);

        inputImage.Mutate(ctx => ctx
            .GaussianBlur(1.5f)
            .Contrast(1.5f)
            .Brightness(1.1f)
            .Grayscale()
            .BinaryThreshold(0.35f));

        Rectangle cardRectangle = FindCardEdges(inputImage);
        if (cardRectangle == Rectangle.Empty)
            return new DetectionResult { Message = "No cards detected." };

        using Image<Rgba32> originalImage = Image.Load<Rgba32>(inputImagePath);
        using Image<Rgba32> croppedImage = CropImage(originalImage, cardRectangle);
        string deckName = DeckResolver.ResolveDeckName(croppedImage);

        originalImage.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Red, 6), cardRectangle));

        var resultImagePath = Path.Combine(UploadsPath, "result_" + fileName);
        originalImage.Save(resultImagePath);

        return new DetectionResult
        {
            DetectedDeck = deckName,
            HighlightedImagePath = $"/uploads/result_{fileName}"
        };
    }

    private static Image<Rgba32> CropImage(Image<Rgba32> image, Rectangle cardRectangle)
    {
        return image.Clone(ctx => ctx.Crop(cardRectangle));
    }

    private static Rectangle FindCardEdges(Image<Rgba32> image)
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

        return (left < right && top < bottom)
            ? new Rectangle(left, top, right - left, bottom - top)
            : Rectangle.Empty;
    }

    private static bool IsBlack(Rgba32 pixel)
    {
        return pixel.R == 0 && pixel.G == 0 && pixel.B == 0;
    }
}