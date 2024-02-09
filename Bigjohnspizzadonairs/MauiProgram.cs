using Bigjohnspizzadonairs.Data;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Bigjohnspizzadonairs
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("MaterialIcons-Regular.ttf", "GoogleFont");

				});

            builder.Services.AddTransient<DbmaManager>();
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
