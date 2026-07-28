using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddScoped<AuthChecker>();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.MapControllers();

// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
//     if (app.Environment.IsDevelopment())
//     {
//         await db.Database.EnsureDeletedAsync();
//         await db.Database.MigrateAsync();
//     }
//     await DataSeeder.SeedAsync(db);
// }

app.Run();
