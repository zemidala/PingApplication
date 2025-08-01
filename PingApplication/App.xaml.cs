using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PingApp.Interfaces;
using PingApp.Services;
using PingApp.ViewModels;

namespace PingApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Создаем и показываем только одно главное окно
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPingService, PingService>();
            services.AddSingleton<IPingStatisticsService, PingStatisticsService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }
    }
}