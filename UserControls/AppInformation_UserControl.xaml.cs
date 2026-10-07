using sapphire_diffmaker.Entities;
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
using System.IO;

namespace sapphire_diffmaker.UserControls
{
    /// <summary>
    /// Interaction logic for AppInformation_UserControl.xaml
    /// </summary>
    public partial class AppInformation_UserControl : UserControl
    {
        private const string _changelogFilePath = "changelog.txt";

        public AppInformation_UserControl()
        {
            InitializeComponent();
            appVersionLabel.Text += App.AppVersion;
            description.Text += App.AppDescription;
            LoadChangeLog();
        }

        private void LoadChangeLog()
        {
            var entries = new List<ChangeLogEntry>();

            if (!File.Exists(_changelogFilePath))
            {
                entries.Add(new ChangeLogEntry { Date = "Ошибка отображения данных" });
                ChangeLogItemsControl.ItemsSource = entries;
                return;
            }

            string content = File.ReadAllText(_changelogFilePath, Encoding.UTF8);
            // Разделяем записи по пустым строкам
            string[] blocks = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in blocks)
            {
                string[] lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var entry = new ChangeLogEntry
                {
                    Date = lines[0].Trim(),
                    Version = lines[1].Trim(),
                    Changes = string.Join("\n", lines.ToList().Skip(2))
                };
                entries.Add(entry);
            }
            ChangeLogItemsControl.ItemsSource = entries;
        }
    }
}
