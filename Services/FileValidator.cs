using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace sapphire_diffmaker.Services
{
    public interface IFileValidator
    {
        (bool IsValidated, string Reason) Validate();
    }

    public abstract class FileValidator: IFileValidator
    {
        protected string _filePath;
        protected abstract string ExpectedElementName { get; }

        public FileValidator(string filePath) 
        {
            _filePath = filePath;
        }

        public (bool IsValidated, string Reason) Validate()
        {
            var res1 = BaseValidation();
            if (!res1.IsValidated)
                return res1;
            var res2 = ExtendedValidation();
            if (!res2.IsValidated)
                return res2;
            return (true, null);
        }

        protected virtual (bool IsValidated, string Reason) BaseValidation()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return (false, $"File was not found: {_filePath}");
                }

                using (var reader = XmlReader.Create(_filePath, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true }))
                {
                    // Проверяем тэг
                    int k = 0;
                    while (reader.Read() && k < 100)
                    {
                        if (reader.NodeType == XmlNodeType.Element &&
                            reader.LocalName == ExpectedElementName)
                            return (true, null);
                        k++;
                    }
                }
                return (false, $"Element  \"{ExpectedElementName}\" was not found in the file");
            }
            catch (Exception ex)
            {
                return (false, $"XML parsing error: {ex.Message}");
            }
        }

        protected abstract (bool IsValidated, string Reason) ExtendedValidation();
    }

    public class FullModelFileValidator : FileValidator
    {
        protected override string ExpectedElementName => "FullModel";
        public FullModelFileValidator(string filePath) : base(filePath) { }
        protected override (bool IsValidated, string Reason) ExtendedValidation() { return (true, null); }
    }

    public class DiffModelFileValidator : FileValidator
    {
        protected override string ExpectedElementName => "DifferenceModel";
        public DiffModelFileValidator(string filePath) : base(filePath) { }
        protected override (bool IsValidated, string Reason) ExtendedValidation() { return (true, null); }
    }

    public class PlugValidator : FileValidator
    {
        protected override string ExpectedElementName => "";
        public PlugValidator(string filePath) : base(filePath) { }

        protected override (bool IsValidated, string Reason) BaseValidation() { return (true, null); }
        protected override (bool IsValidated, string Reason) ExtendedValidation() { return (true, null); }
    }
}
