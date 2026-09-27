using System.ClientModel;
using System.Threading.RateLimiting;
using DotNetEnv;
using DotNetEnv.Configuration;
using ZavaBanking.Web.Data;
using ZavaBanking.Web.Services;

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
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddSingleton<SqlDatabase>();
builder.Services.AddScoped<IChatRepository, SqlChatRepository>();
builder.Services.AddScoped<IChatModel, AzureChatModel>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddRateLimiter(options =>
{
	options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
		context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
		{
			PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
		}));
	options.OnRejected = async (context, token) =>
	{
		context.HttpContext.Response.StatusCode = 429;
		context.HttpContext.Response.Headers.RetryAfter = "60";
		await context.HttpContext.Response.WriteAsJsonAsync(new { code = "rate_limit", message = "Too many requests. Wait one minute, then retry." }, token);
	};
});
var app = builder.Build();

if (args.Contains("--initialize") || args.Contains("--check"))
{
	using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
	var stage = "SQL initialization";
	try
	{
		var database = app.Services.GetRequiredService<SqlDatabase>();
		if (args.Contains("--initialize"))
			await database.InitializeAsync(timeout.Token);
		stage = "SQL verification";
		var count = await database.CheckAsync(timeout.Token);
		if (count == 0) throw new InvalidOperationException("FAQ seed is missing.");
		Console.WriteLine($"SQL schema accessible; {count} FAQ entries ready. Existing chats preserved.");
		if (args.Contains("--check"))
		{
			using var scope = app.Services.CreateScope();
			var repository = scope.ServiceProvider.GetRequiredService<IChatRepository>();
			var model = scope.ServiceProvider.GetRequiredService<IChatModel>();
			stage = "SQL FAQ retrieval";
			var faq = await repository.GetFaqAsync(timeout.Token);
			stage = "Azure OpenAI check";
			Console.WriteLine("Azure OpenAI check: requesting a grounded test reply.");
			await model.ReplyAsync(faq,
				[new(1, Guid.NewGuid(), "user", "What is Zava?", DateTime.UtcNow, "Completed", [])], timeout.Token);
			Console.WriteLine("Azure OpenAI deployment returned a valid grounded response. CES delivery is a separate check.");
		}
	}
	catch (OperationCanceledException)
	{
		var reason = timeout.IsCancellationRequested
			? "The two-minute setup/check deadline expired"
			: "The operation was canceled before the setup/check deadline";
		Console.Error.WriteLine($"{reason} during {stage}. The last progress line identifies the pending operation.");
		if (stage.StartsWith("SQL", StringComparison.Ordinal))
			Console.Error.WriteLine("If no 'connected' line appeared for that operation, check az login, the selected tenant and SQL connectivity. If connected, check SQL waits or blocking. A network socket alone does not confirm SQL authentication completed.");
		Environment.ExitCode = 1;
	}
	catch (ClientResultException error)
	{
		var hint = error.Status switch
		{
			400 => "Check deployment support for chat completions and strict JSON structured outputs.",
			401 => "Check that the API key belongs to the configured Azure OpenAI resource and is valid.",
			403 => "Check resource access policies, key authentication settings and network restrictions.",
			404 => "Check the Azure OpenAI resource endpoint and the deployment name in that same resource.",
			429 => "Check model quota, rate limits and available capacity before retrying.",
			_ => "Check Azure OpenAI service availability, endpoint and network connectivity."
		};
		Console.Error.WriteLine($"Azure OpenAI check failed (HTTP {error.Status}). {hint} SQL initialization, if requested, already completed. No fallback was used.");
		Environment.ExitCode = 1;
	}
	catch (Exception error)
	{
		Console.Error.WriteLine($"Setup/check failed ({error.GetType().Name}). Verify .env or other configuration, az login, database access, schema and model deployment. No fallback was used.");
		Environment.ExitCode = 1;
	}
	return;
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
	app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
	context.Response.Headers.XContentTypeOptions = "nosniff";
	context.Response.Headers.XFrameOptions = "DENY";
	context.Response.Headers["Referrer-Policy"] = "no-referrer";
	context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; img-src 'self'; style-src 'self'; script-src 'self'; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
	await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.MapDefaultControllerRoute();
app.MapGet("/health", () => Results.Ok(new { status = "running", dependenciesChecked = false }));
app.Run();

public partial class Program;