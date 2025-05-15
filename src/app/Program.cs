using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using taskloom.Data;
using taskloom.Services;
using System.Text.Json.Serialization;

namespace taskloom
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<taskloomContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("taskloomContext") ?? throw new InvalidOperationException("Connection string 'taskloomContext' not found.")));

            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                });
            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddSingleton<MailService>();
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddScoped<LogService>();
            builder.Services.AddScoped<PasswordService>();
            builder.Services.AddScoped<UserProjectService>();
            builder.Services.AddScoped<ChartService>();
            builder.Services.AddScoped<SortUsersService>();
            builder.Services.AddScoped<ExcelReportService>();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(90);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseSession();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();



            app.UseAuthorization();

            app.MapRazorPages();

            app.MapControllerRoute(
              name: "default",
              pattern: "{controller=Users}/{action=Login}/{id?}");


         //app.MapFallbackToPage("/Users/Login");

            app.Run();
        }
    }
}
