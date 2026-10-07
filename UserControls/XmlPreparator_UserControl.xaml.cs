using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using sapphire_diffmaker.Entities;
using sapphire_diffmaker.ViewModels;

namespace sapphire_diffmaker.UserControls
{
    /// <summary>
    /// Interaction logic for XmlPreparator_UserControl.xaml
    /// </summary>
    public partial class XmlPreparator_UserControl : UserControl
    {
        private XmlPreparator_ViewModel ViewModel => DataContext as XmlPreparator_ViewModel;
        private ILogger<XmlPreparator_UserControl> _logger;

        public XmlPreparator_UserControl(XmlPreparator_ViewModel viewModel, ILogger<XmlPreparator_UserControl> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
            _logger = logger;
        }

        public void ButtonClick_RemovePath(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                ViewModel.RemovePath(filePath);
            }
        }

        public void ButtonClick_ClearPaths(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearPaths();
        }

        public void ButtonClick_ChoosePaths(object sender, RoutedEventArgs e)
        {
            ViewModel.ChoosePaths();
        }

        public void ButtonClick_MovelUp(object sender, RoutedEventArgs e)
        {
            if (PathListBox.SelectedItem is string path)
            {
                ViewModel.MoveUp(path);
                PathListBox.SelectedItem = path;
            }
        }

        public async void ButtonClick_Execute(object sender, RoutedEventArgs e)
        {
            await ExecuteWithHandlingAsync(
                () => ViewModel.ExecuteSelectedFilesAsync(),
                "Обработка файлов выполнена успешно");
        }

        public async Task ExecuteWithHandlingAsync(Func<Task> action, string rusSuccessMessage)
        {
            try
            {
                _logger.LogInformation("Launching the process of XML editing");
                await action();
                _logger.LogInformation("Operation completed successfully");
                MessageBox.Show(rusSuccessMessage);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation was cancelled by the user");
                MessageBox.Show("Операция была отменена пользователем");
            }
            catch (InvalidLaunchParametersException ex)
            {
                _logger.LogWarning($"Launch error: {ex.Message}");
                MessageBox.Show($"Ошибка запуска: {ex.RusMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Execution error");
                MessageBox.Show("Ошибка выполнения. Подробности в логе работы приложения");
            }
        }

        public void ButtonClick_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelOperation();
        }
    }
}
