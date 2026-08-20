using BacPaymentApi.Models;
using BacPaymentApi.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<BacOptions>(builder.Configuration.GetSection("Bac"));
builder.Services.Configure<SoftlandOptions>(builder.Configuration.GetSection("Softland"));

const string CorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? new[] { "http://localhost:8080" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient<BacApiService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
builder.Services.AddHttpClient<CamtService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
builder.Services.AddScoped<SoftlandService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors(CorsPolicy);
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
