using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GwentCardsDetector.Web.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GwentCardsDetector.Web.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public IFormFile UploadedFile { get; set; }
        public DetectionResult Result { get; private set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a valid image.");
                return Page();
            }

            Result = SingleCardDetector.Detect(UploadedFile);
            return Page();
        }
    }
}
