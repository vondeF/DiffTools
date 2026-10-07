using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Reflection;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using sapphire_diffmaker.Entities;
using sapphire_diffmaker.ViewModels;
using sapphire_diffmaker.UserControls;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.AccessControl;
using System.Security.Principal;


namespace sapphire_diffmaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App :Application
    {
        public static readonly string AppVersion = "1.0.2.1";
        public static readonly string AppDescription = "Утилита для формирования наборов изменений и работы с CIMXML для раскрытия на CIM-портале";
        public static readonly string ConfigPath = "appsettings.json";

        private static string _assemblyResolvePath;
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";

            // Ищем в указанной папке
            string assemblyPath = Path.Combine(_assemblyResolvePath, assemblyName);
            if (!assemblyName.StartsWith("EPPlus"))
            {
                // Пытаемся загрузить из указанной папки
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            // Загружаем локально (из папки с исполняемым файлом)
            string localAssemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
            if (File.Exists(localAssemblyPath))
            {
                return Assembly.LoadFrom(localAssemblyPath);
            }

            // Если сборка не найдена нигде, возвращаем null
            return null;
        }

        static App()
        {
            // === SelfLog: пишем во временную папку пользователя ===
            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                try
                {
                    var selfLogPath = Path.Combine(Path.GetTempPath(), "serilog-selflog.txt");
                    File.AppendAllText(selfLogPath,
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}{Environment.NewLine}");
                }
                catch { /* не роняем приложение из-за SelfLog */ }
            });

            var assemblyDefaultResolvePath = @"C:\Program Files\Monitel\CK-11\Client";
            try
            {
                var json = JObject.Parse(File.ReadAllText(ConfigPath));
                _assemblyResolvePath = json["AppSettings"]?["AssemblyResolvePath"]?.ToString();
                if (_assemblyResolvePath == null)
                    _assemblyResolvePath = assemblyDefaultResolvePath;
            }
            catch
            {
                _assemblyResolvePath = assemblyDefaultResolvePath;
            }
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public App()
        {

        }

        /// <summary>
        /// Создание папки с логами по выбранному пути с правами на запись в файлы внутри этой папки всем пользователям
        /// </summary>
        /// <param name="dir"></param>
        private static void EnsureLogDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
            try
            {
                var di = new DirectoryInfo(dir);
                var sec = di.GetAccessControl();

                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

                var rule = new FileSystemAccessRule(
                    users,
                    FileSystemRights.Modify,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                sec.AddAccessRule(rule);
                di.SetAccessControl(sec);

                // Уже существующие *.log не подхватят новые права сами —
                // наследование не пересчитывается у ранее созданных файлов.
                foreach (var file in Directory.GetFiles(dir, "*.log"))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        var fs = fi.GetAccessControl();
                        fs.AddAccessRule(new FileSystemAccessRule(
                            users,
                            FileSystemRights.Modify,
                            InheritanceFlags.None,
                            PropagationFlags.None,
                            AccessControlType.Allow));
                        fi.SetAccessControl(fs);
                    }
                    catch { /* отдельный файл поправить не удалось — не критично */ }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Serilog.Debugging.SelfLog.WriteLine(
                    $"EnsureLogDirectory: нет прав изменить ACL для '{dir}'");
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine(
                    $"EnsureLogDirectory: ошибка для '{dir}': {ex}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Установка прав на папку с логами
                var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                EnsureLogDirectory(logDir);

                var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(ConfigPath, optional: false, reloadOnChange: true)
                .Build();

                Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

                var settings = configuration.GetSection("AppSettings").Get<AppSettings>();
                _assemblyResolvePath = settings?.AssemblyResolvePath;
                if (string.IsNullOrEmpty(_assemblyResolvePath))
                    throw new Exception("AssemblyResolvePath path for searching libraries has not been set");

                var services = new ServiceCollection();
                services.AddLogging(builder => 
                { 
                    builder.ClearProviders(); 
                    builder.AddSerilog(Log.Logger, dispose: false); 
                });
                services.AddSingleton<AppSettings>(settings);
                services.AddTransient<DiffsCreator_ViewModel>();
                services.AddTransient<XmlPreparator_ViewModel>();
                services.AddTransient<DiffsCreator_UserControl>();
                services.AddTransient<XmlPreparator_UserControl>();
                services.AddTransient<AppInformation_UserControl>();
                services.AddTransient<MainWindow>();

                var serviceProvider = services.BuildServiceProvider();
                var mainWindow = serviceProvider.GetRequiredService<MainWindow>(); // Создание MainWindow с помощью DI
                Log.Information("");
                Log.Information($"Application has been started. Version: {AppVersion}");
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Сritical error occurred while launching the application");
                MessageBox.Show("Критическая ошибка при запуске. Подробности в логе работы приложения");
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application has been closed");
            Log.CloseAndFlush(); // Сбрасывает буфер в файл
            base.OnExit(e);
        }
    }
}
