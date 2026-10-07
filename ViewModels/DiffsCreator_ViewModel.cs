using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Monitel.Mal.Context.CIM16;
using Newtonsoft.Json;
using sapphire_diffmaker.Entities;
using sapphire_diffmaker.Services;
using Microsoft.Win32;
using Monitel.Mal.Meta;
using System.Diagnostics;
using System.Reflection;
using System.Net.NetworkInformation;
using Monitel.Serialization.CIMXML;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace sapphire_diffmaker.ViewModels
{
    public class DiffsCreator_ViewModel: INotifyPropertyChanged
    {
        // Флаг об активности процесса загрузки набора изменений
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        // Статус
        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        // Флаг о доступе функций генерации наборов изменений
        private bool _isDiffCreatorEnabled;
        public bool IsDiffCreatorEnabled
        {
            get => _isDiffCreatorEnabled;
            set
            {
                _isDiffCreatorEnabled = value;
                OnPropertyChanged();
            }
        }

        // Имя сервера
        private string _serverName;
        public string ServerName
        {
            get => _serverName;
        }

        // Список баз данных
        private List<DatabaseInfo> _databases;
        public List<DatabaseInfo> Databases
        {
            get => _databases;
            set
            {
                _databases = value;
                OnPropertyChanged();
            }
        }

        // Выбранная база данных
        private DatabaseInfo _selectedDatabase;
        public DatabaseInfo SelectedDatabase
        {
            get => _selectedDatabase;
            set
            {
                _selectedDatabase = value;
                OnPropertyChanged();
            }
        }

        // Выбранный профиль для выгрузки
        private string _selectedProfilePath;
        public string SelectedProfilePath
        {
            get => _selectedProfilePath;
            set
            {
                _selectedProfilePath = value;
                OnPropertyChanged();
            }
        }

        // Флаг о необходимости выгрузки по профилю
        private bool _isProfileNeeded;
        public bool IsProfileNeeded
        {
            get => _isProfileNeeded;
            set
            {
                _isProfileNeeded = value;
                OnPropertyChanged();
            }
        }

        // Флаг о необходимости игнорирования выбранных классов
        private bool _areSelectedClassesIngnored;
        public bool AreSelectedClassesIngnored
        {
            get => _areSelectedClassesIngnored;
            set
            {
                _areSelectedClassesIngnored = value;
                OnPropertyChanged();
            }
        }

        // Игнорируемые классы
        private List<string> _ignoringClasses;
        public string IgnoringClasses
        {
            get => string.Join(", ", _ignoringClasses);
            set
            {
                _ignoringClasses = value.Split(new string[] { ", " }, StringSplitOptions.None).ToList();
                OnPropertyChanged();
            }
        }

        // Выбран ли экспорт по mRID
        private bool _isMridExport;
        public bool IsMridExport
        {
            get => _isMridExport;
            set
            {
                if (_isMridExport != value)
                {
                    _isMridExport = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRdfProviderSelectionEnabled));
                    SelectedRdfProvider = null;
                }
            }
        }
        public bool IsRdfProviderSelectionEnabled => !_isMridExport;

        // Модули экспорта
        private List<RdfProviderInfo> _rdfProviders;
        public List<RdfProviderInfo> RdfProviders
        {
            get => _rdfProviders;
            set
            {
                _rdfProviders = value;
                OnPropertyChanged();
            }
        }

        // Выбранный модуль экспорта
        private RdfProviderInfo _selectedRdfProvider;
        public RdfProviderInfo SelectedRdfProvider
        {
            get => _selectedRdfProvider;
            set
            {
                _selectedRdfProvider = value;
                OnPropertyChanged();
            }
        }

        // Номер модели ДО
        private int _modelNumberBefore;
        public int ModelNumberBefore
        {
            get => _modelNumberBefore;
            set
            {
                _modelNumberBefore = value;
                OnPropertyChanged();
            }
        }

        // Номер модели ПОСЛЕ
        private int _modelNumberAfter;
        public int ModelNumberAfter
        {
            get => _modelNumberAfter;
            set
            {
                _modelNumberAfter = value;
                OnPropertyChanged();
            }
        }
        
        // Таймер
        private string _elapsedTime;
        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }

        private System.Timers.Timer _timer;
        private Stopwatch _stopwatch;
        private DiffMaker _diffMaker;
        private ModelVersionReceiver _modelVersionReceiver;
        private CancellationTokenSource _cancellationTokenSource;
        private ILogger<DiffsCreator_ViewModel> _logger;
        private string _connectionStringMappingService;
                
        public DiffsCreator_ViewModel(AppSettings appSettings, ILogger<DiffsCreator_ViewModel> logger)
        {
            _isLoading = false;
            _status = "";
            _serverName = appSettings.ServerPath;
            _isDiffCreatorEnabled = true;
            _databases = appSettings.Databases?.OrderBy(x => x.Description).ToList();
            _selectedDatabase = null;
            _isMridExport = false;
            _rdfProviders = new List<RdfProviderInfo>();
            _selectedRdfProvider = null;
            _selectedProfilePath = Path.Combine(AppContext.BaseDirectory, "profile.dat");
            _isProfileNeeded = false;
            _areSelectedClassesIngnored = false;
            _ignoringClasses = new List<string>()
            {
                nameof(ConformLoad),
                nameof(NonConformLoad),
                nameof(StationSupply),
                nameof(TransmissionSLD),
                nameof(SubstationSLD),
                nameof(NetworkView),
                nameof(ConnectivityNode),
                nameof(Terminal)
            };
            _modelNumberBefore = -1;
            _modelNumberAfter = -1;
            _elapsedTime = "00:00:00";

            LoadExportModules(appSettings.ModelNavigatorPath, appSettings.ProvidersDB);
            _modelVersionReceiver = new ModelVersionReceiver();
            _logger = logger;
            _connectionStringMappingService = appSettings.ConnectionStringMappingService;

            // Инициализация таймера
            _stopwatch = new Stopwatch();
            _timer = new System.Timers.Timer(1000); // Обновление каждую секунду
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
        }

        private void LoadExportModules(string modelNavigatorPath, string providersDb)
        {
            var localDiffMaker = new DiffMaker(_serverName, providersDb);
            RdfProviders = localDiffMaker.GetProvidersInfo(modelNavigatorPath).OrderBy(x => x.Name).ToList();
        }

        /// <summary>
        /// Выбор файла профиля
        /// </summary>
        public void ChooseProfile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Выберите файл профиля";
            openFileDialog.Filter = "DAT файлы (*.dat)|*.dat|Все файлы (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
                SelectedProfilePath = openFileDialog.FileName;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ElapsedTime = _stopwatch?.Elapsed.ToString(@"hh\:mm\:ss") ?? "00:00:00";
        }

        private void StartTimer()
        {
            _stopwatch?.Restart();
            _timer?.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _stopwatch?.Stop();
        }

        public async Task SaveDiff_BeforeAfter()
        {
            // Создаем новый токен отмены
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Проверка перед стартом
            if (_selectedDatabase == null)
                throw new InvalidLaunchParametersException("Database has not been selected", "БД не выбрана");
            if (_selectedRdfProvider == null && !_isMridExport)
                throw new InvalidLaunchParametersException("Export provider has not been selected", "Модуль экспорта не выбран");
#if AIP
            if (_selectedRdfProvider != null && _selectedRdfProvider.Name != "Оригинальный CIM 16")
                throw new InvalidLaunchParametersException($"Module {_selectedRdfProvider.Name} cannot be selected for export", $"Модуль {_selectedRdfProvider.Name} не может быть выбран для экспорта");
#endif
            if (_isProfileNeeded && !File.Exists(_selectedProfilePath))
                throw new InvalidLaunchParametersException("Profile file does not exist", "Файл профиля не существует");
            if (_areSelectedClassesIngnored)
            {
                foreach(var className in _ignoringClasses)
                {
                    if(!ClassChecker.IsRightClass(className))
                        throw new InvalidLaunchParametersException($"Class ({className}) does not exist in the canonical model", $"Класс {className} не существует в канонической модели");
                }
            }
            if (!_modelVersionReceiver.IsVersionExists(_serverName, _selectedDatabase.Name, _modelNumberBefore))
                throw new InvalidLaunchParametersException("BEFORE model does not exist", "Модель ДО не существует");
            if (!_modelVersionReceiver.IsVersionExists(_serverName, _selectedDatabase.Name, _modelNumberAfter))
                throw new InvalidLaunchParametersException("AFTER model does not exist", "Модель ПОСЛЕ не существует");

            // Старт
            IsLoading = true;
            IsDiffCreatorEnabled = false;

            // Выбор файла
            Status = "Выбор пути для сохранения...";
            string outputPath = XmlSaver.SelectFile();
            if (string.IsNullOrEmpty(outputPath))
                throw new InvalidLaunchParametersException("Save path has not been selected", "Не выбран путь сохранения");

            // Настройки экспорта
            var diffSettings = new DiffSettings(_modelNumberBefore, _modelNumberAfter)
            {
                RdfProvider = _selectedRdfProvider,
                IsMridExport = _isMridExport,
                IgnoredClasses = _areSelectedClassesIngnored ? _ignoringClasses : null,
                Profile = _isProfileNeeded ? MetaProfile.FromText(File.ReadAllText(_selectedProfilePath)) : null
            };

            // Экспорт набора изменений
            Status = "Формирование набора изменений...";
            StartTimer();

            _diffMaker = new DiffMaker(_serverName, _selectedDatabase.Name);
            _diffMaker.SetConnectionStringMappingService(_connectionStringMappingService);
            try
            {
                await Task.Run(() =>
                {
                    _diffMaker.Compare_BeforeAfter(diffSettings, outputPath, _logger, token);
                }, token);
            }
            finally
            {
                StopTimer();
                ClearElements();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ClearElements()
        {
            IsLoading = false;
            IsDiffCreatorEnabled = true;
            Status = "";
        }

        public void CancelOperation()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource.Cancel();
        }

        // Событие, которое вызывается при изменении свойства ViewModel
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Вызываем событие PropertyChanged с именем свойства, которое изменилось
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
