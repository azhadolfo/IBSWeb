using Google.Apis.Auth.OAuth2;
using IBS.DataAccess.Data;
using IBS.DataAccess.Repository;
using IBS.DataAccess.Repository.IRepository;
using IBS.Services;
using IBS.Utility;
using IBSWeb.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudLogging;

var builder = WebApplication.CreateBuilder(args);

//// Load configuration based on the environment
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


// Configure logging based on environment
if (builder.Environment.IsProduction())
{
    try
    {
        var googleCloudSinkOptions = new GoogleCloudLoggingSinkOptions
        {
            ProjectId = "integrated-business-system",
            LogName = "ibs-web-log",
            ResourceType = "global",
            UseLogCorrelation = true,
            UseSourceContextAsLogName = false
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.GoogleCloudLogging(googleCloudSinkOptions, restrictedToMinimumLevel: LogEventLevel.Warning)
            .CreateLogger();

        // Replace default logging with Serilog
        builder.Host.UseSerilog();
    }
    catch (Exception ex)
    {
        // Fallback logging if Google Cloud Logging fails
        Console.WriteLine($"Failed to configure Google Cloud Logging: {ex.Message}");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
    }
}
else
{
    // Use standard console logging for non-production environments
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();

    builder.Host.UseSerilog();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>().AddDefaultTokenProviders()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHostedService<ExpireUnusedCustomerOrderSlipsService>();
builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
builder.Services.AddScoped<GoogleDriveImportService>();
builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
builder.Services.AddSignalR();

// Add Quartz services
builder.Services.AddQuartz(q =>
{
    // Use DI with Quartz
    q.UseMicrosoftDependencyInjectionJobFactory();

    // Register the job
    var monthlyClosureKey = JobKey.Create(nameof(MonthlyClosureService));
    var dailyPlacementLockKey = JobKey.Create(nameof(LockPlacementService));

    ///TODO Register the job for COS Expiration

    q.AddJob<MonthlyClosureService>(options => options.WithIdentity(monthlyClosureKey));
    q.AddJob<LockPlacementService>(options => options.WithIdentity(dailyPlacementLockKey));

    // Add the first trigger
    // Format (sec, min, hour, day, month, year)
    q.AddTrigger(opts => opts
        .ForJob(monthlyClosureKey)
        .WithIdentity("MonthlyTrigger") // Trigger 1
        .WithCronSchedule("0 0 0 1 * ?",
            x => x.InTimeZone(
                TimeZoneInfo
                    .FindSystemTimeZoneById("Asia/Manila")))); // Run at midnight on the first day of every month

    q.AddTrigger(opts => opts
        .ForJob(dailyPlacementLockKey)
        .WithIdentity("DailyPlacementTrigger")
        .WithCronSchedule("0 0 0 * * ?",
            x => x.InTimeZone(
                TimeZoneInfo
                .FindSystemTimeZoneById("Asia/Manila"))));
});


// Add Quartz Hosted Service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

app.UseSerilogRequestLogging(); // Logs HTTP requests to the terminal

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/User/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//Postgre date and time behaviour
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<MaintenanceMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

// Map SignalR Hub endpoint
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
