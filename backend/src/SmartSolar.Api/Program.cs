using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Repositories;
using SmartSolar.Api.Services;

using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Register MongoDB BSON conventions
var conventionPack = new ConventionPack
{
    new CamelCaseElementNameConvention(),
    new IgnoreExtraElementsConvention(true)
};
ConventionRegistry.Register("SmartSolarConventions", conventionPack, _ => true);

// Bind MongoDB connection settings from configuration (never hard-coded).
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Register single shared MongoClient and IMongoDatabase connection pool
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

// Asynchronously initialize and verify MongoDB indexes on application boot
builder.Services.AddHostedService<MongoIndexInitializer>();

// Register domain repositories
builder.Services.AddSingleton<CounterRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<ProsumerRepository>();

// Register domain services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProsumerService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
        var signingKey = jwtSettings.SigningKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(signingKey)
            || signingKey.Contains("replace-with-a-long-random-value", StringComparison.OrdinalIgnoreCase)
            || signingKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured from an external secret and be at least 32 characters.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse(); // Prevent default 401
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { code = "E4301", message = "Invalid or missing token.", traceId = context.HttpContext.TraceIdentifier });
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { code = "E4303", message = "You do not have permission to perform this action.", traceId = context.HttpContext.TraceIdentifier });
            }
        };
    });
builder.Services.AddAuthorization();

// Register MVC controllers and OpenAPI generation for the Postman/contract work.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartSolar API", Version = "v1" });
    
    // Add the "Authorize" button to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below (without 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Allow the React web client to call the API during development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

// Error envelope middleware must run first so it can catch everything below it.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("WebClient");

app.UseAuthentication();
app.UseMiddleware<StatusCheckMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so the integration test project can spin up the host.
public partial class Program { }
