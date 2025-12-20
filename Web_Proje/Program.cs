using Microsoft.AspNetCore.Identity;
using Web_Proje.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<GymContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();
//Ai servisi ekleniyor.
builder.Services.AddScoped<Web_Proje.Services.AiService>();
builder.Services.AddRazorPages();
builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<GymContext>();
builder.Services.AddHostedService<AppointmentStatusWorker>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Admin rolü yoksa oluştur
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
        }

        // Member rolü yoksa oluştur
        if (!await roleManager.RoleExistsAsync("Member"))
        {
            await roleManager.CreateAsync(new IdentityRole<int>("Member"));
        }

        // Admin kullanıcısı yoksa oluştur
        string adminEmail = "b251210352@sakarya.edu.tr";
        string adminPassword = "Sau1234@";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var newAdmin = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(newAdmin, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                Console.WriteLine("Admin kullanıcısı başarıyla oluşturuldu!");
            }
            else
            {
                Console.WriteLine("Admin kullanıcısı oluşturulamadı:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error.Description}");
                }
            }
        }
        else
        {
            Console.WriteLine("Admin kullanıcısı zaten mevcut.");

            // Admin rolünde değilse ekle
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Mevcut kullanıcıya Admin rolü eklendi!");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed işlemi sırasında hata: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
