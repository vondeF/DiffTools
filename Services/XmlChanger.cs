using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace sapphire_diffmaker.Services
{
    public static class XmlChanger
    {
        /// <summary>
        /// Приводит даты к формату, который нужен для загрузки на CIM-портал
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="progressCallback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task ProcessDatesAsync(string inputPath, string outputPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            var tags = new List<string>
            {
                "rf:LifecycleDate.initialInServiceDate",
                "cim:LifecycleDate.removalDate"
            };

            long fileSize = new FileInfo(inputPath).Length;
            const int bufferSize = 1024 * 1024;
            const long progressUpdateInterval = 10 * 1024 * 1024; // Обновляем прогресс раз в 10 Мб
            long processedBytes = 0;
            long lastProgressUpdate = 0;

            // Преобразуем тэги для ускорения поиска
            var tagPatterns = new List<TagPattern>();
            foreach (var tag in tags)
            {
                tagPatterns.Add(new TagPattern
                {
                    TagName = tag,
                    TagStart = $"<{tag}>",
                    TagEnd = $"</{tag}>"
                });
            }

            // Обработка
            using (var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            using (var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize))
            using (var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: bufferSize))
            {
                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string trimmedLine = line.TrimStart();
                    foreach (var pattern in tagPatterns)
                    {
                        // Если есть нужный тэг
                        if (trimmedLine.StartsWith(pattern.TagStart, StringComparison.Ordinal))
                        {
                            string whiteSpaces = line.Substring(0, line.Length - trimmedLine.Length);
                            int startIndex = whiteSpaces.Length + pattern.TagStart.Length;
                            int endIndex = line.IndexOf(pattern.TagEnd, StringComparison.Ordinal);

                            if (endIndex > startIndex)
                            {
                                string dateValue = line.Substring(startIndex, endIndex - startIndex);

                                // Меняем дату
                                if (dateValue.Length > 0 && dateValue[dateValue.Length - 1] == 'Z' && !dateValue.Contains('T'))
                                {
                                    string newDateValue = dateValue.Substring(0, dateValue.Length - 1) + "T00:00:00Z";
                                    line = whiteSpaces + pattern.TagStart + newDateValue + pattern.TagEnd;
                                }
                            }
                            break; // Ожидаем в строке только один тэг
                        }
                    }

                    await writer.WriteLineAsync(line).ConfigureAwait(false);

                    // Кол-во обработанных байтов
                    processedBytes += Encoding.UTF8.GetByteCount(line) + 2; // +2 for newline

                    if (processedBytes - lastProgressUpdate >= progressUpdateInterval)
                    {
                        double progressValue = Math.Min(1.0, (double)processedBytes / fileSize);
                        progress?.Report(progressValue);
                        lastProgressUpdate = processedBytes;
                    }
                }

                // Финальное обновление прогресса
                progress?.Report(1.0);
            }
        }

        private class TagPattern
        {
            public string TagName { get; set; }
            public string TagStart { get; set; }
            public string TagEnd { get; set; }
        }

        /// <summary>
        /// Меняет префиксы cim на so в соотвествии с GOST.xml
        /// </summary>
        /// <param name="gostXmlPath"></param>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task ProcessSOPrefixAsync(string gostXmlPath, string inputPath, string outputPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            var gostXml = XmlLoader.LoadXmiFromPath(gostXmlPath);
            var gostModel = CanonicalModelXmlParser.Parse(gostXml);
            var soElements = gostModel.GetSoElementsWithouPrefix();

            var prefixDict = soElements.ToDictionary(
                key => $"cim:{key}",
                val => $"so:{val}"
            );

            var keysToCheck = prefixDict.Keys.ToList();
            long fileSize = new FileInfo(inputPath).Length;
            const int bufferSize = 1024 * 1024;
            const long progressUpdateInterval = 10 * 1024 * 1024; // Обновляем прогресс раз в 10 Мб
            long processedBytes = 0;
            long lastProgressUpdate = 0;

            using (var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            using (var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize))
            using (var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: bufferSize))
            {
                // Флаг, чтобы обработать корневой элемент только один раз
                bool rdfNamespaceProcessed = false;
                string soNamespace = "xmlns:so=\"http://so-ups.ru/2015/schema-cim16#\"";

                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Добавляем неймспейсы
                    if (!rdfNamespaceProcessed && line.Contains("<rdf:RDF"))
                    {
                        rdfNamespaceProcessed = true;
                        if (!line.Contains("xmlns:so=")) // Проверяем, есть ли неймспейс
                        {
                            // Вставляем namespace перед закрывающим '>' корневого элемента.
                            int last = line.LastIndexOf('>');
                            if (last != -1)
                                line = line.Insert(last, " " + soNamespace);
                        }
                    }

                    // Проверяем, есть ли в строке хоть один из ключей, прежде чем запускать логику
                    bool needsReplacement = false;
                    foreach (var key in keysToCheck)
                    {
                        int keyIndex = line.IndexOf(key, StringComparison.Ordinal);
                        if (keyIndex != -1 &&
                            (keyIndex + key.Length >= line.Length || line[keyIndex + key.Length] != '.')) // Ключ найден И после него нет точки
                        {
                            needsReplacement = true;
                            break;
                        }
                    }

                    if (needsReplacement)
                    {
                        // Проходим по словарю и делаем замены
                        foreach (var kvp in prefixDict)
                        {
                            if (line.Contains(kvp.Key))
                                line = line.Replace(kvp.Key, kvp.Value);
                        }
                    }

                    await writer.WriteLineAsync(line).ConfigureAwait(false);

                    processedBytes += Encoding.UTF8.GetByteCount(line) + 2; // +2 for newline (\r\n or \n)

                    if (processedBytes - lastProgressUpdate >= progressUpdateInterval)
                    {
                        double progressValue = Math.Min(1.0, (double)processedBytes / fileSize);
                        progress?.Report(progressValue);
                        lastProgressUpdate = processedBytes;
                    }
                }

                // Финальное обновление прогресса
                progress?.Report(1.0);
            }
        }

    }
}
