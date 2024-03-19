using Bigjohnspizzadonairs.Data;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static Bigjohnspizzadonairs.Pages.ShiftDetais;

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
            builder.Services.AddSingleton<IFileSaveService, FileSaveService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
