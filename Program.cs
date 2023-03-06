// dotnet new webapp
// "@{ Layout = "/Views/Shared/_Layout.cshtml"; }" | Out-File -FilePath Areas\Identity\Pages\_ViewStart.cshtml
// dotnet ef migrations add InitialCreate
// dotnet ef database update
// dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
// git update-index --assume-unchanged .\appsettings.json

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Regit.Data;
using Microsoft.AspNetCore.Authorization;
using Regit.Authorization;

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

builder.Services.AddControllersWithViews();

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
await SeedData.Initialize(services, testUserPw, domain);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); //

app.Run();
