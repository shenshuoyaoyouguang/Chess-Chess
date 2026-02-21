using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SharkChess.Services;

namespace SharkChess
{
    /// <summary>
    /// 应用程序入口
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 服务提供者
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// 应用启动
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // 加载保存的主题
            var themeService = Services.GetRequiredService<IThemeService>();
            themeService.LoadSavedTheme();

            // 显示主窗口
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // 服务
            services.AddSingleton<Services.IGameService, Services.GameService>();
            services.AddSingleton<Services.IEngineService, Services.EngineService>();
            services.AddSingleton<Services.IThemeService, Services.ThemeService>();

            // 窗口和视图模型
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ViewModels.MainViewModel>();
        }
    }
}
