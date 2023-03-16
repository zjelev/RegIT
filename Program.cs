// dotnet new mvc -au Individual
// dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
// dotnet add package Microsoft.EntityFrameworkCore.SqlServer
// dotnet aspnet-codegenerator controller -name ContractsController -m Contract -dc Regit.Data.ApplicationDbContext --relativeFolderPath Controllers --referenceScriptLibraries
// dotnet aspnet-codegenerator identity -dc Regit.Data.ApplicationDbContext --files "Account.Register;Account.Login"
// "@{ Layout = "/Views/Shared/_Layout.cshtml"; }" | Out-File -FilePath Areas\Identity\Pages\_ViewStart.cshtml
// dotnet ef migrations add Initial -o Data\Migrations
// dotnet ef database update
// git update-index --assume-unchanged .\appsettings.json

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Regit.Data;
using Microsoft.AspNetCore.Authorization;
using Regit.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true; //sendgrid
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 4;
        options.Password.RequiredUniqueChars = 0;
        options.Lockout.DefaultLockoutTimeSpan = new TimeSpan(0, 2, 0);
        options.Lockout.MaxFailedAccessAttempts = 8;
        //options.User.RequireUniqueEmail = true; //Initially throws Exception at SeedData.cs:line 66
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// builder.Services.Configure<IdentityOptions>(options => { });

builder.Services.AddLocalization(); // options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.SetDefaultCulture("en-US");
        options.AddSupportedUICultures("bg");
        options.FallBackToParentUICultures = true;
        //options.RequestCultureProviders.Clear();
    });

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() //all users to be authenticated, except with [AllowAnonymous]
        .Build()
    );

// Authorization handlers.
builder.Services.AddScoped<IAuthorizationHandler, IsOwnerAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AdministratorsAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ManagerAuthorizationHandler>();


var app = builder.Build();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
var context = services.GetRequiredService<ApplicationDbContext>();
context.Database.Migrate();
// Set password with the Secret Manager tool. dotnet user-secrets set SeedUserPW <pw>
var testUserPw = builder.Configuration.GetValue<string>("SeedUserPW");
var domain = builder.Configuration.GetValue<string>("domain");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await SeedData.Initialize(services, testUserPw, domain);
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
    app.UseDatabaseErrorPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Default HSTS value is 30 days. To change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}

app.UseHttpsRedirection(); //letsencrypt
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
//app.UseAuthentication();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); //

app.Run();
