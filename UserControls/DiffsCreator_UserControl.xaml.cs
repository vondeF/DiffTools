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
    /// Interaction logic for DiffsCreator_UserControl.xaml
    /// </summary>
    public partial class DiffsCreator_UserControl : UserControl
    {
        private DiffsCreator_ViewModel ViewModel => DataContext as DiffsCreator_ViewModel;
        private ILogger<DiffsCreator_UserControl> _logger;
        public DiffsCreator_UserControl(DiffsCreator_ViewModel viewModel, ILogger<DiffsCreator_UserControl> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
            _logger = logger;
        }

        public void ButtonClick_ChooseProfile(object sender, RoutedEventArgs e)
        {
            ViewModel.ChooseProfile();
        }

        public async void ButtonClick_BeforeAfter(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Launching the process of generating the Difference Model");
                await ViewModel.SaveDiff_BeforeAfter();
                _logger.LogInformation("Difference Model has been successfully formed");
                MessageBox.Show("Набор изменений успешно сформирован");
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
                MessageBox.Show($"Ошибка выполнения. Подробности в логе работы приложения");
            }
        }

        public void ButtonClick_Cancel(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelOperation();
        }
    }
}
