using Microsoft.Win32;
using Monitel.Mal;
using Monitel.Serialization.CIMXML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace sapphire_diffmaker.Services
{
    public static class XmlSaver
    {
        public static void SaveDiffModel(DifferenceModel difModel, string path)
        {
            using (var sw = new StreamWriter(path))
            {
                DmSerializationExtension.ExportToXml(difModel, XmlWriter.Create(sw, new XmlWriterSettings() { Indent = true }));
            }
            MessageBox.Show("Файл сохранен");
        }

        public static string SelectFile()
        {
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Задайте наименование файла для сохранения",
                DefaultExt = "xml",
                AddExtension = true,
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*"
            };

            bool? result = sfd.ShowDialog();

            return result == true ? sfd.FileName : null;
        }

        public static string SelectFolder()
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Выберите папку для сохранения"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                return dialog.FileName;

            return null;
        }
    }
}
