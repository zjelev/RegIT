using Regit.Authorization;
using Regit.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Regit.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw, string domain)
    {
        using var context = new ApplicationDbContext(serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());
        // For sample purposes seed both with the same password.
        // Password is set with the following: dotnet user-secrets set SeedUserPW <pw>
        // The admin user can do anything
        var adminID = await EnsureUser(serviceProvider, testUserPw, "admin@" + domain);
        await EnsureRole(serviceProvider, adminID, Constants.AdministratorsRole);

        // allowed user can create and edit contacts that they create
        var managerID = await EnsureUser(serviceProvider, testUserPw, "manager@" + domain);
        await EnsureRole(serviceProvider, managerID, Constants.ManagersRole);

        SeedDB(context, adminID);
    }

    private static async Task<string> EnsureUser(IServiceProvider serviceProvider, string testUserPw, string UserName)
    {
        var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

        var user = await userManager.FindByNameAsync(UserName);
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = UserName,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, testUserPw);
        }

        if (user == null)
            throw new Exception("The password is probably not strong enough!");

        return user.Id;
    }

    private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider, string uid, string role)
    {
        var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

        if (roleManager == null)
            throw new Exception("roleManager null");

        IdentityResult IR;
        if (!await roleManager.RoleExistsAsync(role))
            IR = await roleManager.CreateAsync(new IdentityRole(role));

        var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

        if (userManager == null)
            throw new Exception("userManager is null");

        var user = await userManager.FindByIdAsync(uid);

        if (user == null)
            throw new Exception("The testUserPw password was probably not strong enough!");

        IR = await userManager.AddToRoleAsync(user, role);

        return IR;
    }

    private static void SeedDB(ApplicationDbContext context, string adminID)
    {
        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department
                {
                    Name = "МО"
                },
                new Department
                {
                    Name = "ЕО"
                },
                new Department
                {
                    Name = "ИО"
                },
                new Department
                {
                    Name = "АО"
                },
                new Department
                {
                    Name = "ИВ"
                }
            );
            context.SaveChanges();
        }

        if (!context.Contracts.Any())
        {
            context.Contracts.AddRange(
                new Contract
                {
                    SignedOn = DateOnly.FromDateTime(DateTime.Now),
                    Subject = "Обучение",
                    ValidFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                    RegNum = "TO 1/2023",
                    Value = 2337.99M,
                    Responsible = context.Departments.Where(d => d.Name == "АО").FirstOrDefault(),
                    Owner = context.Users.FirstOrDefault()
                },
                new Contract
                {
                    SignedOn = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    Subject = "Доставка на метали",
                    ValidFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                    RegNum = "TO 2/2023",
                    Value = 3537.99M,
                    Responsible = context.Departments.Where(d => d.Name == "МО").FirstOrDefault(),
                    Owner = context.Users.OrderBy(u => u.UserName).FirstOrDefault()
                },
                new Contract
                {
                    SignedOn = DateOnly.FromDateTime(DateTime.Now.AddDays(4)),
                    Subject = "Поддръжка на оборудване",
                    ValidFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
                    RegNum = "VD 3/2023",
                    Value = 2337.99M,
                    Responsible = context.Departments.Where(d => d.Name == "ИО").FirstOrDefault(),
                    Owner = context.Users.OrderBy(u => u.UserName).LastOrDefault()

                },
                new Contract
                {
                    SignedOn = DateOnly.FromDateTime(DateTime.Now.AddDays(6)),
                    Subject = "Доставка на ел. матeриали",
                    ValidFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                    RegNum = "TO 4/2023",
                    Value = 2337.99M,
                    Responsible = context.Departments.Where(d => d.Name == "ЕО").FirstOrDefault(),
                    Owner = context.Users.OrderBy(u => u.UserName).FirstOrDefault()
                },
                new Contract
                {
                    SignedOn = DateOnly.FromDateTime(DateTime.Now.AddDays(8)),
                    Subject = "Строителство",
                    ValidFrom = DateOnly.FromDateTime(DateTime.Now.AddDays(9)),
                    RegNum = "TO 21/2023",
                    Value = 2337.99M,
                    Responsible = context.Departments.Where(d => d.Name == "ИВ").FirstOrDefault(),
                    Owner = context.Users.OrderBy(u => u.UserName).LastOrDefault()
                }
            );
            context.SaveChanges();
        }
    }
}