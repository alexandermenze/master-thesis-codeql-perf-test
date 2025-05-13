using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CustomerPortal.CustomerWebsite.Pages;

public class Index : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/PriceLists");
}
