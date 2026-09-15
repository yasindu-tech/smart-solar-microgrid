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

using SmartSolar.Api.Configuration;
using SmartSolar.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Bind MongoDB connection settings from configuration (never hard-coded).
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("Mongo"));

// Register MVC controllers and OpenAPI generation for the Postman/contract work.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapControllers();

app.Run();

// Exposed so the integration test project can spin up the host.
public partial class Program { }
