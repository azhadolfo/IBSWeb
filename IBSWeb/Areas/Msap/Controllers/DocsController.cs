using IBS.Models;
using IBS.Services.Msap.AccessControl;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace IBSWeb.Areas.Msap.Controllers;

[Area("Msap")]
[Route("Msap/Docs")]
[Authorize]
public class DocsController(
    IWebHostEnvironment env,
    UserManager<ApplicationUser> userManager,
    IAccessControlService accessControl) : Controller
{
    private static readonly string[] ManualFiles =
    [
        "00-user-login",
        "01-job-order",
        "02-dispatch-ticket",
        "03-billing",
        "04-collection",
        "05-service-request",
        "06-master-files",
        "07-admin",
        "08-import-export",
        "09-reports",
    ];

    private readonly string _docsRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "Docs", "manual"));
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var html = RenderMarkdownFile("README");
        ViewBag.Sidebar = await GetSidebarItems();
        ViewBag.ActiveSlug = "index";
        ViewBag.Title = "User Manual";
        return View("Index", html);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Show(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return RedirectToAction(nameof(Index));
        }

        if (slug == "index" || slug == "README")
        {
            return RedirectToAction("Index");
        }

        var fileSlug = ManualFiles.FirstOrDefault(f =>
            f.EndsWith(slug, StringComparison.OrdinalIgnoreCase) ||
            f.Replace("0", "").Replace("-", "") == slug.Replace("-", ""));

        if (fileSlug == null)
        {
            return NotFound();
        }

        if (!await UserCanAccess(fileSlug))
        {
            return NotFound();
        }

        var html = RenderMarkdownFile(fileSlug);
        if (html == null)
        {
            return NotFound();
        }

        ViewBag.Sidebar = await GetSidebarItems();
        ViewBag.ActiveSlug = slug;
        ViewBag.Title = GetTitleFromFile(fileSlug);
        return View("Index", html);
    }

    private string? RenderMarkdownFile(string slug)
    {
        var filePath = slug == "README"
            ? Path.Combine(_docsRoot, "README.md")
            : Path.Combine(_docsRoot, $"{slug}.md");

        if (!System.IO.File.Exists(filePath))
        {
            return null;
        }

        var markdown = System.IO.File.ReadAllText(filePath);
        return Markdown.ToHtml(markdown, _pipeline);
    }

    private static string GetTitleFromFile(string slug)
    {
        var match = Regex.Match(slug, @"^\d+-(.+)$");
        if (!match.Success)
        {
            return "User Manual";
        }

        var name = match.Groups[1].Value.Replace("-", " ");
        return char.ToUpper(name[0]) + name[1..];
    }

    private async Task<List<(string Slug, string Title)>> GetSidebarItems()
    {
        var userId = userManager.GetUserId(User)!;
        var allItems = new List<(string Slug, string Title, Func<string, Task<bool>>? Guard)>
        {
            ("index", "Overview", null),
            ("user-login", "User Login", null),
            ("job-order", "Job Order", null),
            ("dispatch-ticket", "Dispatch Ticket", null),
            ("billing", "Billing", null),
            ("collection", "Collection", null),
            ("service-request", "Service Request", null),
            ("master-files", "Master Files", null),
            ("admin", "Administration", async _ => User.IsInRole("Admin")),
            ("import-export", "Import & Export", async uid => await accessControl.HasMsapImportAccessAsync(uid)),
            ("reports", "Reports", async uid => await accessControl.HasMaritimeReportAccessAsync(uid))
        };

        var result = new List<(string Slug, string Title)>();
        foreach (var (slug, title, guard) in allItems)
        {
            if (guard == null || await guard(userId))
            {
                result.Add((slug, title));
            }
        }
        return result;
    }

    private async Task<bool> UserCanAccess(string fileSlug)
    {
        var userId = userManager.GetUserId(User)!;
        return fileSlug switch
        {
            "07-admin" => User.IsInRole("Admin"),
            "08-import-export" => await accessControl.HasMsapImportAccessAsync(userId),
            "09-reports" => await accessControl.HasMaritimeReportAccessAsync(userId),
            _ => true
        };
    }
}
