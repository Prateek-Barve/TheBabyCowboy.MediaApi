using Microsoft.EntityFrameworkCore;
using TheBabyCowBoy.MediaApi.Data;
using TheBabyCowBoy.MediaApi.Services;
using TheBabyCowBoy.MediaApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
//var clerkIssuer = builder.Configuration["Clerk:Issuer"];
//var allowedUserId = builder.Configuration["Clerk:AllowedUserId"];

//if (string.IsNullOrWhiteSpace(clerkIssuer) ||
//    string.IsNullOrWhiteSpace(allowedUserId))
//{
//    throw new InvalidOperationException(
//        "Clerk:Issuer and Clerk:AllowedUserId must be configured.");
//}

//var authorizedParties =
//    builder.Configuration
//        .GetSection("Clerk:AuthorizedParties")
//        .Get<string[]>()
//    ?? Array.Empty<string>();

//builder.Services
//    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.Authority = clerkIssuer;
//        options.MapInboundClaims = false;

//        options.TokenValidationParameters =
//            new TokenValidationParameters
//            {
//                ValidateIssuer = true,
//                ValidIssuer = clerkIssuer,

//                // Clerk's default session token may not contain an audience.
//                ValidateAudience = false,

//                ValidateLifetime = true,
//                ValidateIssuerSigningKey = true
//            };

//        options.Events = new JwtBearerEvents
//        {
//            OnTokenValidated = context =>
//            {
//                var azp = context.Principal?
//                    .FindFirst("azp")?.Value;

//                // If azp exists, it must match a configured frontend origin.
//                if (!string.IsNullOrEmpty(azp) &&
//                    !authorizedParties.Contains(
//                        azp,
//                        StringComparer.OrdinalIgnoreCase))
//                {
//                    context.Fail(
//                        "The token was issued for an unauthorized frontend.");
//                }

//                return Task.CompletedTask;
//            }
//        };
//    });

//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminOnly", policy =>
//    {
//        policy.RequireAuthenticatedUser();

//        policy.RequireAssertion(context =>
//            context.User.FindFirst("sub")?.Value == allowedUserId);
//    });
//});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

var entraAuthority = builder.Configuration["Entra:Authority"];
var mediaApiClientId = builder.Configuration["Entra:MediaApiClientId"];
var mediaApiAudience = builder.Configuration["Entra:Audience"];
var allowedAdminObjectId =
    builder.Configuration["Entra:AllowedAdminObjectId"];

if (string.IsNullOrWhiteSpace(entraAuthority) ||
    string.IsNullOrWhiteSpace(mediaApiClientId) ||
    string.IsNullOrWhiteSpace(mediaApiAudience) ||
    string.IsNullOrWhiteSpace(allowedAdminObjectId))
{
    throw new InvalidOperationException(
        "Entra authentication settings are missing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = entraAuthority;
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,
                ValidAudiences = new[]
                {
                    mediaApiClientId,
                    mediaApiAudience
                },

                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireAssertion(context =>
            string.Equals(
                context.User.FindFirst("oid")?.Value,
                allowedAdminObjectId,
                StringComparison.OrdinalIgnoreCase));
    });
});



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
                "https://127.0.0.1:5173",
                "https://blue-ground-0be55f310.5.azurestaticapps.net"
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
