using Google.Cloud.Storage.V1;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Models;
using IBS.Services;
using IBS.Utility;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// DBContext (scoped)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// Razor
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

// Repositories + DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
builder.Services.AddScoped<GoogleDriveImportService>();
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IUserAccessService, UserAccessService>();
builder.Services.AddScoped<IHubConnectionRepository, HubConnectionRepository>();
builder.Services.AddScoped<IMonthlyClosureService, MonthlyClosureService>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<ISubAccountResolver, SubAccountResolver>();
builder.Services.AddScoped<StartOfTheMonthService>();
builder.Services.AddScoped<DailyService>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB cap
});

// Use LocalFileStorageService for Development, CloudStorageService for Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ICloudStorageService, LocalFileStorageService>();
}
else
{
    builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
}

if (builder.Environment.IsProduction())
{
    var bucketName = builder.Configuration["GoogleCloudStorageBucketName"]!;
    var storageClient = StorageClient.Create();

    builder.Services.AddDataProtection()
        .SetApplicationName("IBS-Web")
        .AddKeyManagementOptions(options =>
        {
            options.XmlRepository = new GcsXmlRepository(
                storageClient,
                bucketName,
                "dataprotection-keys.xml"
            );
        });
}

var app = builder.Build();

app.MapPost("/jobs/start-of-the-month-service",
    async (StartOfTheMonthService service) =>
    {
        await service.Execute(null!);
        return Results.Ok("StartOfTheMonthService job executed.");
    })
    .AllowAnonymous();

app.MapPost("/jobs/daily-service",
    async (DailyService service) =>
    {
        await service.Execute(null!);
        return Results.Ok("DailyService job executed.");
    })
    .AllowAnonymous();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/User/Home/Error");
    app.UseHsts();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseStaticFiles();

// Enable serving files from local storage in development
if (app.Environment.IsDevelopment())
{
    var localStoragePath = Path.Combine(app.Environment.ContentRootPath, "App_Data", "LocalStorage");
    if (!Directory.Exists(localStoragePath))
    {
        Directory.CreateDirectory(localStoragePath);
    }
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(localStoragePath),
        RequestPath = "/local-storage"
    });
}

app.UseMiddleware<MaintenanceMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

// SignalR
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
