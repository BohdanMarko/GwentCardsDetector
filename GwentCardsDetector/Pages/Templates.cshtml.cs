using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GwentCardsDetector.Pages;

public class TemplateViewModel
{
    public string DeckName { get; set; }
    public string ImagePath { get; set; }
}

public sealed class TemplatesModel : PageModel
{
    public List<TemplateViewModel> Templates { get; set; } = [];

    private const string TemplatesPath = "wwwroot/templates/";

    public void OnGet()
    {
        var templateFiles = Directory.GetFiles(TemplatesPath);

        foreach (var filePath in templateFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            Templates.Add(new TemplateViewModel
            {
                DeckName = MapTemplateNameToDeckName(fileName),
                ImagePath = $"/templates/{Path.GetFileName(filePath)}"
            });
        }
    }

    private string MapTemplateNameToDeckName(string templateName) => templateName switch
    {
        "monsters-card" => "�������",
        "nilfgaard-card" => "�i��������",
        "redaniya-card" => "�����i�",
        "scoiatael-card" => "����������",
        "skellige-card" => "����i��",
        "temeriya-card" => "�����i�",
        "toussaint-card" => "�������",
        "velen-card" => "�����",
        "wild_hunt-card" => "���� �����",
        "witchers-card" => "�i������",
        _ => "���i���� ������"
    };
}
