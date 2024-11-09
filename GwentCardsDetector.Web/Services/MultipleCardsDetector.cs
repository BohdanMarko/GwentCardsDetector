using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using GwentCardsDetector.Web.Services;

namespace GwentCardsDetector;

public sealed class MultipleCardsDetector
{
    private const string UploadsPath = "wwwroot/uploads";

    public static DetectionResult Detect(IFormFile uploadedFile)
    {
        if (uploadedFile == null || uploadedFile.Length == 0)
            return new DetectionResult { Message = "Bad input image." };

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

        List<Rectangle> cardRectangles = FindAllCardEdges(inputImage);

        if (cardRectangles.Count == 0)
        {
            return new DetectionResult { Message = "No cards detected." };
        }

        Dictionary<string, int> deckCards = new Dictionary<string, int>();
        foreach (Rectangle card in cardRectangles)
        {
            using Image<Rgba32> originalImage = Image.Load<Rgba32>(inputImagePath);
            using Image<Rgba32> croppedImage = CropImage(originalImage, card);
            string deckName = DeckResolver.ResolveDeckName(croppedImage);
            if (deckCards.TryGetValue(deckName, out int value))
            {
                deckCards[deckName] = ++value;
            }
            else
            {
                deckCards.Add(deckName, 1);
            }
        }

        using Image<Rgba32> finalImage = Image.Load<Rgba32>(inputImagePath);
        cardRectangles.ForEach(card => finalImage.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Red, 6), card)));
        string resultImagePath = Path.Combine(UploadsPath, "result_" + fileName);
        finalImage.Save(resultImagePath);

        return new DetectionResult
        {
            HighlightedImagePath = $"/uploads/result_{fileName}",
            TotalCards = cardRectangles.Count,
            DeckCards = deckCards
        };
    }

    private static Image<Rgba32> CropImage(Image<Rgba32> image, Rectangle cardRectangle)
    {
        return image.Clone(ctx => ctx.Crop(cardRectangle));
    }

    static List<Rectangle> FindAllCardEdges(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        bool[,] visited = new bool[width, height];
        List<Rectangle> cardRectangles = [];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsBlack(image[x, y]) && !visited[x, y])
                {
                    Rectangle cardRectangle = FloodFill(image, visited, x, y);
                    if (IsValidCardSize(cardRectangle))
                    {
                        cardRectangles.Add(cardRectangle);
                    }
                }
            }
        }

        return cardRectangles;
    }

    // way too simple
    static bool IsValidCardSize(Rectangle rect) => rect.Width < 150 || rect.Height < 150 ? false : true;

    static Rectangle FloodFill(Image<Rgba32> image, bool[,] visited, int startX, int startY)
    {
        int width = image.Width;
        int height = image.Height;
        int left = startX, right = startX, top = startY, bottom = startY;

        Queue<(int x, int y)> queue = [];
        queue.Enqueue((startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();

            left = Math.Min(left, x);
            right = Math.Max(right, x);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);

            foreach ((int nx, int ny) in GetNeighbors(x, y, width, height))
            {
                if (IsBlack(image[nx, ny]) && !visited[nx, ny])
                {
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return new Rectangle(left, top, right - left + 1, bottom - top + 1);
    }

    static IEnumerable<(int x, int y)> GetNeighbors(int x, int y, int width, int height)
    {
        if (x > 0) yield return (x - 1, y);
        if (x < width - 1) yield return (x + 1, y);
        if (y > 0) yield return (x, y - 1);
        if (y < height - 1) yield return (x, y + 1);
    }

    static bool IsBlack(Rgba32 pixel) => pixel.R == 0 && pixel.G == 0 && pixel.B == 0;
}
