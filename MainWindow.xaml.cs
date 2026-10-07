using sapphire_diffmaker.Entities;
using sapphire_diffmaker.UserControls;
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

namespace sapphire_diffmaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(DiffsCreator_UserControl diffsControl, XmlPreparator_UserControl xmlControl, AppInformation_UserControl infoControl)
        {
            InitializeComponent();
#if AIP
            tabDiffs.Content = diffsControl;
            tabXml.Visibility = Visibility.Collapsed;
            tabInf.Visibility = Visibility.Collapsed;
#else
            tabDiffs.Content = diffsControl;
            tabXml.Content = xmlControl;
            tabInf.Content = infoControl;
#endif
        }
    }
}
