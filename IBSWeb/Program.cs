using Google.Cloud.Storage.V1;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.DataAccess.Repository.MasterFile;
using IBS.DataAccess.Repository.MasterFile.IRepository;
using IBS.Models;
using IBS.Services;
using IBS.Services.Attributes;
using IBS.Utility;
using IBS.Utility.Helpers;
using IBSWeb.Api;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF;
using QuestPDF.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Configuration.AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// QuestPDF
Settings.License = LicenseType.Community;

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
IMvcBuilder razorPagesBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
    razorPagesBuilder.AddRazorRuntimeCompilation();
}

// Repositories + DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IHubConnectionRepository, HubConnectionRepository>();
builder.Services.AddScoped<IMonthlyClosureService, MonthlyClosureService>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
builder.Services.AddScoped<ISubAccountResolver, SubAccountResolver>();
builder.Services.AddScoped<ITransactionMasterControlService, TransactionMasterControlService>();
builder.Services.AddScoped<StartOfTheMonthService>();
builder.Services.AddScoped<DailyService>();

// SignalR
builder.Services.AddSignalR();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB cap
});

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

builder.Services.AddScoped<IAuthorizationHandler, DepartmentHandler>();

builder.Services.AddScoped<IDepartmentAccessRepository, DepartmentAccessRepository>();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();

builder.Services.AddAuthorization();

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

app.MapGet("/health", () => Results.Ok("Healthy")).AllowAnonymous();

var dmcmApiKey = builder.Configuration["ApiKeys:DMCM_API_KEY"];
if (string.IsNullOrWhiteSpace(dmcmApiKey))
{
    throw new InvalidOperationException("ApiKeys:DMCM_API_KEY is not configured");
}

app.MapJournalVoucherEndpoints(dmcmApiKey);

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/User/Home/Error");
    app.UseHsts();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<MaintenanceMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

// SignalR
app.MapHub<NotificationHub>("/notificationHub");

// Initialize the IConfiguration to get the calendarific_api_key
DateTimeHelper.Initialize(app.Configuration);

app.Run();
