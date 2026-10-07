using Monitel.Mal.Context.CIM16;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Services
{
    public static class DiffModelXmlChanger
    {
        public static async Task ProcessSectionName(string inputPath, string outputPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            const int bufferSize = 1024 * 1024;
            progress?.Report(0);

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

                    if (trimmedLine.Contains("<dm:forwardDifferences") && !trimmedLine.Contains("/>"))
                        await writer.WriteLineAsync("    <dm:forwardDifferences rdf:parseType=\"Statements\" xml:base=\"https://cim.so-ups.ru#\">").ConfigureAwait(false);
                    else if (trimmedLine.Contains("<dm:reverseDifferences") && !trimmedLine.Contains("/>"))
                        await writer.WriteLineAsync("    <dm:reverseDifferences rdf:parseType=\"Statements\" xml:base=\"https://cim.so-ups.ru#\">").ConfigureAwait(false);
                    else
                        await writer.WriteLineAsync(line).ConfigureAwait(false);
                }

                // Финальное обновление прогресса
                progress?.Report(1.0);
            }
        }


        public static async Task ProcessAddingAssocGeoRegionAsync(string inputPath, string outputPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            long fileSize = new FileInfo(inputPath).Length;
            const int bufferSize = 1024 * 1024;
            const long progressUpdateInterval = 10 * 1024 * 1024; // Обновляем прогресс раз в 10 Мб

            // ---------- ПРОХОД 1: сбор данных ----------
            var forward = new DifferenceSectionInfo();
            var reverse = new DifferenceSectionInfo();

            long processedBytes = 0;
            long lastProgressUpdate = 0;

            using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
            using (var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize))
            {
                DifferenceSectionInfo currentSection = null;
                string currentUid = null;
                string previousUid = null;

                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("<dm:forwardDifferences"))
                        currentSection = forward;
                    else if (trimmed.StartsWith("<dm:reverseDifferences"))
                        currentSection = reverse;

                    if (currentSection == null)
                        continue;

                    previousUid = currentUid;
                    if (trimmed.Contains("rdf:about") && !trimmed.Contains("dm:DifferenceModel"))
                    {
                        currentUid = ExtractUid(line);
                        currentSection.Objects.Add(currentUid); // Собираем Uid всех объектов
                    }
                        
                    if (currentUid == null)
                        continue;
                    
                    // находимся внутри объекта
                    if (previousUid == currentUid)
                    {
                        if (line.Contains("cim:SubGeographicalRegion.Line"))
                            currentSection.AssocPairs.Add(ExtractUid(line), currentUid);
                    }

                    processedBytes += Encoding.UTF8.GetByteCount(line) + 2;
                    if (processedBytes - lastProgressUpdate >= progressUpdateInterval)
                    {
                        // Первый проход — 50% общего прогресса
                        double p = Math.Min(0.5, 0.5 * (double)processedBytes / fileSize);
                        progress?.Report(p);
                        lastProgressUpdate = processedBytes;
                    }
                }
            }

            // ---------- Определяем, каких объектов не хватает ----------
            foreach (var pair in forward.AssocPairs)
            {
                if (!forward.Objects.Contains(pair.Key))
                    forward.MissingObjects.Add(pair.Key);
                else
                    forward.ExistingObjects.Add(pair.Key);
            }

            foreach (var pair in reverse.AssocPairs)
            {
                if (!reverse.Objects.Contains(pair.Key))
                    reverse.MissingObjects.Add(pair.Key);
                else
                    reverse.ExistingObjects.Add(pair.Key);
            }

            // ---------- ПРОХОД 2: запись со вставкой ассоциаций ----------
            processedBytes = 0;
            lastProgressUpdate = 0;
            using (var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            using (var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize))
            using (var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: bufferSize))
            {
                DifferenceSectionInfo currentSection = null;
                string currentUid = null;
                bool isAssocAdded = false;

                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string trimmed = line.TrimStart();

                    if (trimmed.StartsWith("<dm:forwardDifferences"))
                        currentSection = forward;
                    else if (trimmed.StartsWith("<dm:reverseDifferences"))
                        currentSection = reverse;

                    // Перед закрытием секции дописываем недостающие объекты
                    if (trimmed.StartsWith("</dm:forwardDifferences>") || trimmed.StartsWith("</dm:reverseDifferences>"))
                    {
                        if (currentSection == forward && currentSection.MissingObjects.Count > 0)
                        {
                            foreach (var uid in currentSection.MissingObjects)
                                await WriteNewDescriptionAsync(writer, uid, currentSection.AssocPairs[uid]);
                        }
                        if (currentSection == reverse && currentSection.MissingObjects.Count > 0)
                        {
                            foreach (var uid in currentSection.MissingObjects)
                                await WriteNewDescriptionAsync(writer, uid, currentSection.AssocPairs[uid]);
                        }
                    }

                    await writer.WriteLineAsync(line).ConfigureAwait(false);

                    if (trimmed.Contains("rdf:about") && !trimmed.Contains("dm:DifferenceModel"))
                    {
                        currentUid = ExtractUid(line);
                        isAssocAdded = false;
                    }
                        
                    if (currentUid != null && currentSection.ExistingObjects.Contains(currentUid) && !isAssocAdded)
                    {
                        await writer.WriteLineAsync($"        <cim:Line.Region rdf:resource=\"#_{currentSection.AssocPairs[currentUid]}\"/>").ConfigureAwait(false);
                        isAssocAdded = true;
                    }

                    processedBytes += Encoding.UTF8.GetByteCount(line) + 2;
                    if (processedBytes - lastProgressUpdate >= progressUpdateInterval)
                    {
                        // Второй проход — оставшиеся 50%
                        double p = 0.5 + Math.Min(0.5, 0.5 * (double)processedBytes / fileSize);
                        progress?.Report(p);
                        lastProgressUpdate = processedBytes;
                    }
                }
                progress?.Report(1.0);
            }
        }

        private static string ExtractUid(string line)
        {
            string marker = "";
            if (line.Contains("rdf:about"))
                marker = "rdf:about=\"#_";
            else if (line.Contains("rdf:resource"))
                marker = "rdf:resource=\"#_";
            else 
                return null;

            int idx = line.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;
            int start = idx + marker.Length;
            int end = line.IndexOf('"', start);
            return end > start ? line.Substring(start, end - start) : null;
        }

        private static async Task WriteNewDescriptionAsync(StreamWriter writer, string uid, string subRegionUid)
        {
            await writer.WriteLineAsync($"      <rdf:Description rdf:about=\"#_{uid}\">").ConfigureAwait(false);
            await writer.WriteLineAsync($"        <cim:Line.Region rdf:resource=\"#_{subRegionUid}\"/>").ConfigureAwait(false);
            await writer.WriteLineAsync("      </rdf:Description>").ConfigureAwait(false);
        }

        private class DifferenceSectionInfo
        {
            public HashSet<string> Objects = new HashSet<string>();

            // Найденные пары Line - SubGeographicalRegion
            public Dictionary<string, string> AssocPairs = new Dictionary<string, string>();

            public HashSet<string> MissingObjects = new HashSet<string>();
            public HashSet<string> ExistingObjects = new HashSet<string>();
        }
    }
}
