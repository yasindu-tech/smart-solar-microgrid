// ---------------------------------------------------------------------------
//  File        : Program.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Host
//  Description : Application entry point. Registers controllers, the MongoDB
//                settings, CORS for the web client and the error envelope
//                middleware. ALL business logic lives in Services - see
//                docs/architecture/system-overview.md section 3.
//  Author      : <member name - M1>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SmartSolar.Api.Configuration;
using SmartSolar.Api.Middleware;
using SmartSolar.Api.Repositories;
using SmartSolar.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind MongoDB connection settings from configuration (never hard-coded).
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Register domain repositories
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
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
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGci...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
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
