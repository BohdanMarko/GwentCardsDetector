using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GwentCardsDetector.Web.Services;

namespace GwentCardsDetector.Web.Pages;

public sealed class IndexModel(CardsDetector cardsDetector) : PageModel
{
    [BindProperty]
    public IFormFile UploadedFile { get; set; }

    public DetectionResult Result { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            ModelState.AddModelError("Error", "Please upload a valid image.");
            return Page();
        }

        Result = await cardsDetector.Detect(UploadedFile);
        return Page();
    }
}
