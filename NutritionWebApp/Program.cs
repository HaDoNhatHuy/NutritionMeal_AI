using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Services;
using System;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
// DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Session (để lưu UserId sau login)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Đăng ký Service API YouTube
//builder.Services.AddHttpClient<IExerciseDbService, ExerciseDbService>();
builder.Services.AddScoped<IExerciseDbService, ExerciseDbService>();
builder.Services.AddHttpClient<IYoutubeService, YoutubeDataService>();
// Sử dụng AddHttpClient để tự động quản lý HttpClient instance

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// FIX LỖI TIMEOUT KHI GỌI AI (Tăng lên 5 phút)
builder.Services.AddHttpClient("FlaskAI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000");
    client.Timeout = TimeSpan.FromMinutes(5); // Tăng timeout
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware để detect mobile và chuyển layout
app.Use(async (context, next) =>
{
    var userAgent = context.Request.Headers["User-Agent"].ToString();
    var isMobile = userAgent.Contains("Mobile") ||
                   userAgent.Contains("Android") ||
                   userAgent.Contains("iPhone");

    context.Items["IsMobile"] = isMobile;
    await next();
});

app.UseRouting();

app.UseSession();  // Dùng Session

app.UseAuthorization();

app.MapControllerRoute(
    name: "root",
    pattern: "/",
    defaults: new { controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
