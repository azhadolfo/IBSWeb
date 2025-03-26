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

var googleCloudSinkOptions = new GoogleCloudLoggingSinkOptions
{
    ProjectId = "integrated-business-system",
    LogName = "ibs-web-log",
    ResourceType = "global",
    UseLogCorrelation = true,  // Enable correlation IDs
    UseSourceContextAsLogName = false  // Keep the log name you specified
};

// Log.Logger = new LoggerConfiguration()
//     .MinimumLevel.Information()  // Sets the minimum level for ALL sinks to Debug
//     .WriteTo.Console()  // This sink will log Debug and above
//     .WriteTo.GoogleCloudLogging(googleCloudSinkOptions, restrictedToMinimumLevel: LogEventLevel.Warning)  // This sink will only log Warning and above
//     .CreateLogger();

// Replace default logging with Serilog
builder.Host.UseSerilog();

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

    ///TODO Register the job for COS Expiration

    q.AddJob<MonthlyClosureService>(options => options.WithIdentity(monthlyClosureKey));

    // Add the first trigger
    // Format (sec, min, hour, day, month, year)
    q.AddTrigger(opts => opts
        .ForJob(monthlyClosureKey)
        .WithIdentity("MonthlyTrigger") // Trigger 1
        .WithCronSchedule("0 0 0 1 * ?",
            x => x.InTimeZone(
                TimeZoneInfo
                    .FindSystemTimeZoneById("Asia/Manila")))); // Run at midnight on the first day of every month
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
