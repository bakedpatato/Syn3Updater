using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Cyanlabs.Syn3Updater
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost host;

        public static IServiceProvider ServiceProvider { get; private set; }
        #region Methods

        protected override async void OnStartup(StartupEventArgs e)
        {
            await host.StartAsync();
            base.OnStartup(e);
            {
                DispatcherUnhandledException += App_DispatcherUnhandledException;
            }
            ApplicationManager.Instance.Initialize();

        }

        public App()
        {
            host = Host.CreateDefaultBuilder()
          .ConfigureServices((context, services) =>
          {
              ConfigureServices(context.Configuration, services);
          }).ConfigureLogging(logging => { })
          .Build();

            ServiceProvider = host.Services;
        }

        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {

            // Register all ViewModels.
            services.AddSingleton<UI.Tabs.HomeViewModel>();
            // Register all the Windows of the applications.
            services.AddTransient<UI.MainWindow>();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ApplicationManager.Logger.CrashWindow(e.Exception);
            e.Handled = true;
        }

        private async void App_OnExit(object sender, ExitEventArgs e)
        {
            using (host)
            {
                await host.StopAsync(System.TimeSpan.FromSeconds(5));
            }
            //  throw new NotImplementedException();
        }

        #endregion
    }
}