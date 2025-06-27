using LocationHeatMapApp.Services;
using Microsoft.Extensions.Logging;

namespace LocationHeatMapApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiMaps() //Maui Map
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

			string dbPath = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");

        builder.Services.AddSingleton(new DatabaseService(dbPath));
        builder.Services.AddSingleton<LocationService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
