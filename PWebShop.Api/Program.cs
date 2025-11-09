using Microsoft.EntityFrameworkCore;
using PWebShop.Infrastructure;
using PWebShop.Domain.Entities;


var builder = WebApplication.CreateBuilder(args);

// database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// controllers en swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// database automatisch aanmaken voor nu
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Categories.Any())
    {
        var cat = new Category { Name = "Test category" };
        db.Categories.Add(cat);

        db.Products.Add(new Product
        {
            Name = "Test product",
            Description = "First seeded product",
            BasePrice = 10m,
            MarkupPercentage = 20m,
            IsActive = true,
            Category = cat
        });

        db.SaveChanges();
    }
}


// middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// later komen hier authentication en authorization
app.MapControllers();

// eenvoudige test endpoint
app.MapGet("/api/ping", () => Results.Ok(new { message = "Api is alive" }));

app.Run();