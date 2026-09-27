using System.Text.Json;
using DotNetEnv;
using DotNetEnv.Configuration;
using ZavaSupport.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var packagedEnvFile = Path.Combine(builder.Environment.ContentRootPath, ".env");
var developmentEnvFile = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../.env"));
var envFile = File.Exists(packagedEnvFile) ? packagedEnvFile : developmentEnvFile;
if (File.Exists(envFile))
{
    try
    {
        builder.Configuration.AddDotNetEnv(envFile, LoadOptions.NoEnvVars());
    }
    catch (Exception)
    {
        throw new InvalidOperationException("Could not load the .env file. Check its syntax and file permissions without sharing its contents.");
    }
}
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
builder.Services.AddSingleton(EventHubSettings.Read(builder.Configuration));
builder.Services.AddSingleton<EscalationStore>();
builder.Services.AddHostedService<EscalationFeedWorker>();

var app = builder.Build();
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; img-src 'self'; style-src 'self'; script-src 'self'; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'none'";
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/escalations/stream", async (HttpContext context, EscalationStore store, CancellationToken token) =>
{
    context.Response.Headers.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers["X-Accel-Buffering"] = "no";
    using var subscription = store.Subscribe();
    try
    {
        await foreach (var message in subscription.Reader.ReadAllAsync(token))
        {
            await context.Response.WriteAsync($"event: {message.Name}\ndata: {JsonSerializer.Serialize(message.Payload, json)}\n\n", token);
            await context.Response.Body.FlushAsync(token);
        }
    }
    catch (OperationCanceledException)
    {
        // The support agent closed the page.
    }
});
app.MapGet("/health", () => Results.Ok(new { status = "running" }));
app.Run();
