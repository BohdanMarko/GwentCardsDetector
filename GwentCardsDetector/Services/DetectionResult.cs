namespace GwentCardsDetector.Web.Services;

public sealed class DetectionResult
{
    public string HighlightedImagePath { get; set; } = string.Empty;
    public int TotalCards { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, int> DeckCards { get; set; } = [];
}