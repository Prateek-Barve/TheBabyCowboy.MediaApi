using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Data;
using TheBabyCowBoy.MediaApi.Services;
using TheBabyCowBoy.MediaApi.Services.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("MediaDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'MediaDatabase' was not configured.");

builder.Services.AddDbContext<MediaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IGenreService, GenreService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "TheBabyCowboy.MediaApi v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("ReactFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
