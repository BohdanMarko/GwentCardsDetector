using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Diagnostics;

namespace GwentCardsDetector;

static class MultipleCardsDetector
{
    const string InputImagePath = @"E:\source\projects\GwentCardsDetector\input\{0}";
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

        List<Rectangle> cardRectangles = FindAllCardEdges(inputImage);

        if (cardRectangles.Count == 0)
        {
            Console.WriteLine("Карт не знайдено");
            return;
        }

        using Image<Rgba32> finalImage = Image.Load<Rgba32>(inputImagePath);
        cardRectangles.ForEach(card => finalImage.Mutate(ctx => ctx.Draw(Pens.Solid(Color.Red, 6), card)));
        finalImage.Save(ResultImagePath);

        Process.Start(new ProcessStartInfo(ResultImagePath) { UseShellExecute = true });
        Console.WriteLine($"Знайдено {cardRectangles.Count} карт");
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
