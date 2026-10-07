using Microsoft.Win32;
using sapphire_diffmaker.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Timers;
using Microsoft.Extensions.Logging;
using sapphire_diffmaker.Entities;
using Monitel.PlatformInfrastructure.TextTools;

namespace sapphire_diffmaker.ViewModels
{
    public class XmlPreparator_ViewModel : INotifyPropertyChanged
    {

        // Флаг об активности процесса
        private bool _isLoading = false;
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

        // Флаг о доступе функций
        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        // Коллекция путей к файлам фрагментов
        private ObservableCollection<string> _filePaths;
        public ObservableCollection<string> FilePaths
        {
            get => _filePaths;
            set
            {
                _filePaths = value;
                OnPropertyChanged();
            }
        }

        // Коллекция шагов обработки
        private ObservableCollection<ProcessingStep> _steps;
        public ObservableCollection<ProcessingStep> Steps
        {
            get => _steps;
            set 
            { 
                _steps = value; 
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
        private CancellationTokenSource _cancellationTokenSource;
        private ILogger<XmlPreparator_ViewModel> _logger;
        private string _gostXmlPath;

        public XmlPreparator_ViewModel(AppSettings appSettings, ILogger<XmlPreparator_ViewModel> logger)
        {
            _isLoading = false;
            _status = "";
            _isEnabled = true;
            _filePaths = new ObservableCollection<string>();
            _gostXmlPath = appSettings.GostXmlPath;
            _elapsedTime = "00:00:00";

            _logger = logger;

            // Шаги обработки
            _steps = new ObservableCollection<ProcessingStep>
            {
                new ProcessingStep
                {
                    Title = "FullModel и DiffModel: Изменение префиксов cim -> so (GOST.xml)",
                    Suffix = "_prefix",
                    Validator = path =>
                    {
                        var result = new PlugValidator(path).Validate();
                        return (result.IsValidated, result.Reason);
                    },
                    Processor = (input, output, progress, token) =>
                        XmlChanger.ProcessSOPrefixAsync(_gostXmlPath, input, output, progress, token)
                },
                new ProcessingStep
                {
                    Title = "FullModel и DiffModel: Изменение дат",
                    Suffix = "_dates",
                    Validator = path =>
                    {
                        var result = new PlugValidator(path).Validate();
                        return (result.IsValidated, result.Reason);
                    },
                    Processor = (input, output, progress, token) =>
                        XmlChanger.ProcessDatesAsync(input, output, progress, token)
                },
                new ProcessingStep
                {
                    Title = "DiffModel: Заполнение ассоциации cim:Line.Region",
                    Suffix = "_assocGeo",
                    Validator = path =>
                    {
                        var result = new DiffModelFileValidator(path).Validate();
                        return (result.IsValidated, result.Reason);
                    },
                    Processor = (input, output, progress, token) =>
                        DiffModelXmlChanger.ProcessAddingAssocGeoRegionAsync(input, output, progress, token)
                },
                new ProcessingStep
                {
                    Title = "DiffModel: Изменение тегов forwardDifferences и reverseDifferences",
                    Suffix = "_sectionTags",
                    Validator = path =>
                    {
                        var result = new DiffModelFileValidator(path).Validate();
                        return (result.IsValidated, result.Reason);
                    },
                    Processor = (input, output, progress, token) =>
                        DiffModelXmlChanger.ProcessSectionName(input, output, progress, token)
                }
            };
            _steps = new ObservableCollection<ProcessingStep>(_steps.OrderBy(x => x.Title));

            // Инициализация таймера
            _stopwatch = new Stopwatch();
            _timer = new System.Timers.Timer(1000); // Обновление каждую секунду
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
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

        public void RemovePath(string path)
        {
            _filePaths.Remove(path);
        }

        public void ClearPaths()
        {
            _filePaths.Clear();
        }

        public void ChoosePaths()
        {
            ChooseFiles(ref _filePaths);
        }

        public void MoveUp(string path)
        {
            var index = _filePaths.IndexOf(path);
            if (index > 0)
            {
                _filePaths.Move(index, index - 1);
            }
        }

        public async Task ExecuteSelectedFilesAsync()
        {
            var selected = Steps.Where(s => s.IsSelected).ToArray();
            if (selected.Length == 0)
                throw new InvalidLaunchParametersException("No processing steps have been selected", "Не выбрано ни одного шага обработки");

            if (_filePaths.Count == 0)
                throw new InvalidLaunchParametersException("No files have been selected", "Файлы не выбраны");

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            string workDir = null;

            try
            {
                // 1. Валидация
                IsLoading = true;
                IsEnabled = false;
                Status = $"Валидация файлов ...";

                var processors = _steps.Where(x => x.IsSelected).ToList();
                var validators = _steps.Where(x => x.Validator != null && x.IsSelected).Select(x => x.Validator).ToList();
                foreach (var path in _filePaths)
                {
                    token.ThrowIfCancellationRequested();
                    foreach (var validator in validators)
                    {
                        var result = validator(path);
                        if (!result.IsValidated)
                            throw new InvalidLaunchParametersException($"The file \"{path}\" failed validation. Reason: {result.Reason}", $"Файл \"{path}\" не прошел валидацию. Причина: {result.Reason}");
                    }
                }
                _logger.LogInformation("Validation of the selected files was successful");

                // 2. Выбор папки
                Status = $"Выбор папки сохранения...";
                var folder = XmlSaver.SelectFolder();
                if (folder == null)
                    throw new InvalidLaunchParametersException("Folder for saving has not been selected", "Папка для сохранения не выбрана");
                _logger.LogInformation($"Folder for saving has been selected: {folder}");

                // Создаем временную папку, чтобы быстрее переносить файлы
                workDir = Path.Combine(folder, "_temp_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(workDir);

                // 3. Обработка
                StartTimer();
                for (int i = 0; i < _filePaths.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var path = _filePaths[i];
                    _logger.LogInformation($"Start processing the file {path}");
                    var fileName = Path.GetFileName(path);
                    
                    await ProcessSingleFileAsync(path, folder, workDir, processors, i, _filePaths.Count, token);
                    _logger.LogInformation($"File {path} has been successfully processed");
                }
            }
            finally
            {
                StopTimer();
                ClearElements();

                // Чистим временную папку
                if (workDir != null && Directory.Exists(workDir))
                {
                    try { Directory.Delete(workDir, true); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete work directory"); }
                }

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task ProcessSingleFileAsync(string inputPath, string outputFolder, string workDir, IReadOnlyList<ProcessingStep> steps, int fileIndex, int filesCount, CancellationToken token)
        {
            // Результирующее имя и путь
            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            var suffixPart = string.Concat(steps.Select(s => s.Suffix));
            var finalName = baseName + suffixPart + ext;
            var finalPath = Path.Combine(outputFolder, finalName);

            string currentInput = inputPath;
            string currentTemp = null;

            try
            {
                for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
                {
                    token.ThrowIfCancellationRequested();
                    var step = steps[stepIndex];

                    // Промежуточный файл
                    var stepOutput = Path.Combine(
                        workDir,
                        $"file_{fileIndex:D4}_step_{stepIndex:D2}{ext}");

                    // Сохраняем, чтобы корректно отобразилось в UI
                    int capturedIndex = stepIndex;
                    var capturedStep = step;
                    var progress = new Progress<double>(p =>
                    {
                        Status = $"{p:P2} | Обработчик {capturedIndex + 1} из {steps.Count} | Файл {fileIndex + 1} из {filesCount}";
                    });
                    await step.Processor(currentInput, stepOutput, progress, token);
                    _logger.LogInformation($"Processor \"{Translator.ToLatin(capturedStep.Title)}\" ({capturedIndex + 1}/{steps.Count}) has finished its work");

                    // Удаляем предыдущий промежуточный файл сразу после успешного шага
                    if (currentTemp != null && File.Exists(currentTemp))
                    {
                        try { File.Delete(currentTemp); }
                        catch (Exception ex) { _logger.LogWarning(ex, $"Failed to delete temp file {currentTemp}"); }
                    }

                    currentTemp = stepOutput;
                    currentInput = stepOutput;
                }

                if (File.Exists(finalPath))
                    File.Delete(finalPath);

                // Переносим файл
                File.Move(currentTemp, finalPath);
                currentTemp = null;
            }
            finally
            {
                if (currentTemp != null && File.Exists(currentTemp))
                {
                    try { File.Delete(currentTemp); }
                    catch (Exception ex) { _logger.LogWarning(ex, $"Failed to delete temp file {currentTemp}"); }
                }
            }
        }

        private void ClearElements()
        {
            IsLoading = false;
            IsEnabled = true;
            Status = "";
        }

        private void ChooseFiles(ref ObservableCollection<string> paths)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var path in dialog.FileNames)
                    paths.Add(path);
            }
        }

        public void CancelOperation()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        // Событие, которое вызывается при изменении свойства ViewModel
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Вызываем событие PropertyChanged с именем свойства, которое изменилось
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProcessingStep : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        // Заголовок чекбокса в UI
        public string Title { get; set; }

        // Приписка к итоговому имени файла
        public string Suffix { get; set; }

        // Валидатор входного файла. Может быть null, тогда шаг пропускает валидацию
        public Func<string, (bool IsValidated, string Reason)> Validator { get; set; }

        public Func<string, string, IProgress<double>, CancellationToken, Task> Processor { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
