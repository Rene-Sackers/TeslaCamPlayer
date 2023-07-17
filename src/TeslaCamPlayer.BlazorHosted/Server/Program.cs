using Serilog;
using Serilog.Events;
using TeslaCamPlayer.BlazorHosted.Server.Providers;
using TeslaCamPlayer.BlazorHosted.Server.Providers.Interfaces;
using TeslaCamPlayer.BlazorHosted.Server.Services;
using TeslaCamPlayer.BlazorHosted.Server.Services.Interfaces;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Is(LogEventLevel.Verbose)
	.WriteTo.Console()
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();
builder.Services.AddTransient<IClipsService, ClipsService>();
#if WINDOWS
builder.Services.AddTransient<IFfProbeService, FfProbeServiceWindows>();
#elif DOCKER
builder.Services.AddTransient<IFfProbeService, FfProbeServiceDocker>();
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();