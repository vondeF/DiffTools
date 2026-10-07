using Microsoft.Win32;
using Monitel.Mal.Meta.Entities;
using Monitel.Mal.Meta;
using Monitel.Mal;
using Monitel.ObjectDb.Client.Rmq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Monitel.Serialization.CIMXML;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using Monitel.Mal.Xml;
using Monitel.Mal.Utils;
using Monitel.Mal.Providers.Mal;
using Monitel.Mal.Providers;
using Monitel.Mal.Meta.Compare;
using sapphire_diffmaker.Entities;
using System.Text.RegularExpressions;
using Monitel.Serialization.CIMXML.Providers;
using Monitel.Serialization.CIMXML.DiffModel;
using Monitel.PlatformInfrastructure.Logger;
using Microsoft.Extensions.Logging;
using sapphire_diffmaker.ViewModels;
using Monitel.MappingService.Client;
using Monitel.ObjectDb.Models;
using Monitel.DataContext.Tools;
using Monitel.DataContext.Tools.Dm;
using Monitel.ObjectDb.Primitives.Utils;
using Monitel.Localization;

namespace sapphire_diffmaker.Services
{

    public class DiffMaker
    {
        private string _serverName;
        private string _database;
        private string _connectionStringMappingService;
        private string _moduleInfo = "DiffTools";

        public DiffMaker(string serverName, string database)
        {
            _serverName = serverName;
            _database = database;
        }

        public void SetConnectionStringMappingService(string connectionStringMappingService)
        {
            _connectionStringMappingService = connectionStringMappingService;
        }

        /// <summary>
        /// Формирует и сохраняет набор изменений с выбранными настройками
        /// </summary>
        /// <param name="diffSettings"></param>
        /// <param name="filepath"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="InvalidLaunchParametersException"></exception>
        public void Compare_BeforeAfter(DiffSettings diffSettings, string filepath, ILogger<DiffsCreator_ViewModel> logger, CancellationToken cancellationToken = default)
        {
            DifferenceModel difModel;
            logger.LogInformation($"Settings: Export provider = {Translator.ToLatin(diffSettings.RdfProvider.Name)}, Using mRID (additionally) = {(diffSettings.IsMridExport ? "Yes" : "No")}, Using profile = {(diffSettings.Profile != null ? "Yes" : "No")}, Ignoring choosen classes = {(diffSettings.IgnoredClasses != null ? "Yes" : "No")}");


            // ======= Формирование базового набора изменений без применения по mRID или Uid =======
            using (var client = OdbClientFactory.CreateClient())
            {
                // 1. Подключение к серверу и БД
                ObjectDatabase instance;
                try { instance = client.GetInstanceByName(_serverName, _database); }
                catch { throw new InvalidLaunchParametersException($"Unable to connect to the server {_serverName} and database {_database}. Check the connection settings", $"Не удалеось подключиться к серверу {_serverName} и базе данных {_database}. Проверьте настройки подключения"); }
                logger.LogInformation($"ObjectDatabase object has been created: Server = {_serverName}, Database = {_database}");

                // 2. Получение метаданных модели после изменений 
                var modelImageAfter = GetModelImage(diffSettings.ModelNumberAfter);
                var metaData = modelImageAfter.MetaData;
                modelImageAfter.Dispose();
                logger.LogInformation($"Metadata object has been received");

                // 3. Настройка профиль
                MetaProfile profile = diffSettings.Profile;

                // 4. Формирование набора
                logger.LogInformation($"Launching the creation of a DifferenceModel object between models {diffSettings.ModelNumberBefore} and {diffSettings.ModelNumberAfter}");
                using (var fs = new MemoryStream())
                {
                    OdbObjectsHistory history;
                    if (diffSettings.IsMridExport)
                        history = client.GetModelVersionDifference(instance, metaData, profile, null, diffSettings.ModelNumberAfter, diffSettings.ModelNumberBefore, true, fs, cancellationToken);
                    else
                        history = client.GetModelVersionMridDifference(instance, metaData, profile, null, diffSettings.ModelNumberAfter, diffSettings.ModelNumberBefore, true, fs, cancellationToken);
                    difModel = history.ExportToDifferenceModel(_moduleInfo, true, client, instance);
                }
                logger.LogInformation($"DifferenceModel object has been created");
            }
            cancellationToken.ThrowIfCancellationRequested();

            // ======= Обработка набора изменений процессором экспорта =======
            // 1. Удаление изменений по игнорируемым классам
            if (diffSettings.IgnoredClasses != null)
            {
                difModel = CleanDiff(difModel, diffSettings.IgnoredClasses);
                logger.LogInformation($"DifferenceModel object has been cleared of the selected ignored classes: {string.Join(", ", diffSettings.IgnoredClasses)}");
            }

            // 2. Получение параметров для сериализации
            var platformLogger = new PlatformLoggerAdapter(logger); // Оборачиваем логгер в интерфейс Монитор электрик
            ServiceMappingCollection mappingCollection = null;
            IDiffExportProcessor processor = null;

#if !AIP
            if (diffSettings.RdfProvider != null && diffSettings.RdfProvider.Name != "Оригинальный CIM 16")
            {
                if (string.IsNullOrEmpty(_connectionStringMappingService))
                    throw new Exception("The connection string to MappingService has not been specified");

                var provider = RdfProviderManager.CreateProviderObject(diffSettings.RdfProvider);
                processor = provider.CreateDiffExportProcessor();
                mappingCollection = new ServiceMappingCollection(_connectionStringMappingService, platformLogger);
            }
#endif

            // 3. Сериализация с выбранным процессором
            try
            {
                logger.LogInformation($"Starting XML generation");
                using (var saveStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                {
                    // Настройка записи в xml
                    XmlWriterSettings xws = new XmlWriterSettings
                    {
                        OmitXmlDeclaration = false,
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        CheckCharacters = false
                    };

                    // Настройка сериализации
                    DmSerializeOptions dso = new DmSerializeOptions
                    {
                        Processor = processor,
                        MappingsCollection = mappingCollection,
                        SourceModel = GetModelImage(diffSettings.ModelNumberBefore),
                        Logger = platformLogger,
                        Errors = new List<string>(),
                    };

                    // Экспорт в файл
                    using (var xwriter = XmlWriter.Create(saveStream, xws))
                        difModel.ExportToXml(xwriter, dso);
                    logger.LogInformation($"The resulting XML is formed according to the following path: {filepath}");
                }
            }
            finally
            {
                if (mappingCollection != null)
                    mappingCollection.Dispose();
            }
        }

        /// <summary>
        /// Функция для фильтрации набора по выбранным классам
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        private XElement BuildCorrectionTemplate(DifferenceModel dm)
        {
            var template = new XElement("correctiontemplate",
                new XAttribute("version", "0.1"),
                new XAttribute("contextname", "CIM16"),
                new XAttribute("guid", Guid.NewGuid().ToString()),
                new XAttribute("name", "DefaultTemplate"),
                new XAttribute("isdefault", "True"));

            var includedClassNames = dm.Forward.All.Union(dm.Reverse.All).Select(x => x.ObjectClass?.Name).Where(x => x != null).ToHashSet();

            foreach (var cls in includedClassNames)
                template.Add(new XElement("class.include", new XAttribute("class", cls)));

            return template;
        }

        /// <summary>
        /// Очистка набора изменений от выбранных классов
        /// </summary>
        /// <param name="dm"></param>
        /// <param name="ignoredClasses"></param>
        /// <returns></returns>
        private DifferenceModel CleanDiff(DifferenceModel dm, List<string> ignoredClasses)
        {
            List<DifferenceObject> removeForwardList = new List<DifferenceObject>();
            List<DifferenceObject> removeReverseList = new List<DifferenceObject>();

            foreach (var k in ignoredClasses)
            {
                var forward = dm.Forward.All.Where(x => x.ObjectClass.Name == k).ToDictionary(x => x.ObjectUid, x => x);
                var reverse = dm.Reverse.All.Where(x => x.ObjectClass.Name == k).ToDictionary(x => x.ObjectUid, x => x);
                var intersected = forward.Keys.Intersect(reverse.Keys).ToList();
                removeForwardList.AddRange(forward.Where(x => intersected.Contains(x.Key)).Select(x => x.Value));
                removeReverseList.AddRange(forward.Where(x => intersected.Contains(x.Key)).Select(x => x.Value));
            }
            foreach (var d in removeForwardList)
                dm.Forward.Delete(d);
            foreach (var d in removeReverseList)
                dm.Reverse.Delete(d);

            return dm;
        }

        /// <summary>
        /// Возвращает список провайдеров для экспорта и импорта
        /// </summary>
        /// <returns></returns>
        public List<RdfProviderInfo> GetProvidersInfo(string modelNavigatorPath)
        {
            var providers = new List<RdfProviderInfo>();
            try
            {
                using (var client = OdbClientFactory.CreateClient())
                {
                    var instance = client.GetInstanceByName(_serverName, _database);
                    var metaData = MetaSource.Factory(client.GetMetaPacket(instance));
                    providers =  RdfProviderManager.GetProviders(metaData, modelNavigatorPath).ToList<RdfProviderInfo>();
                }
            }
            catch (Exception)
            {
                throw new Exception($"Error loading export providers");
            }
            return providers;
        }

        /// <summary>
        /// Загрузка ModelImage
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="dbName"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        private ModelImage GetModelImage(int modelId)
        {
            ModelImage modelImage = null;
            try
            {
                MalContextParams contextParams = new MalContextParams()
                {
                    OdbServerName = _serverName,
                    OdbInstanseName = _database,                
                    OdbModelVersionId = modelId
                };
                MalProvider dataProvider = new MalProvider(contextParams, MalContextMode.Open, _moduleInfo);
                modelImage = new ModelImage(dataProvider);
            }
            catch (Exception ex)
            {
                if (modelImage != null)
                    modelImage.Dispose();
                throw new Exception($"Error loading ModelImage (Server = {_serverName}; Database = {_database}; Id = {modelId}). Message: {ex.Message}");
            }
            return modelImage;
        }
    }
}
