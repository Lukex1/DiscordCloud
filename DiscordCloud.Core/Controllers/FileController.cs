using DiscordCloud.Core.DiscordBotService;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using DiscordCloud.Hash;
using System.Security.Cryptography.Xml;

namespace DiscordCloud.Core.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("DiscordCloud")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly DiscordBot _discordBot;
        private readonly HttpClient _httpClient;
        private readonly string pass = "SET YOUR OWN HASH PASSWORD";
        public FileController(DiscordBot discordBot, IHttpClientFactory httpClientFactory)
        {
            _discordBot = discordBot;
            _httpClient = httpClientFactory.CreateClient(); // Tworzenie instancji HttpClient
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Nie wybrano plików.");
            }

            var encryptor = new Encryptor();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest($"Plik {file.FileName} jest pusty.");
                }

                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        // Zaszyfruj plik bez odczytu danych do tablicy fileBytes
                        byte[] encryptedData = encryptor.EncryptFile(stream, this.pass);

                        // Sprawdzamy, czy plik już istnieje na Discordzie
                        if (await FileExistsOnDiscord(file.FileName))
                        {
                            return Conflict($"Plik o nazwie '{file.FileName}' już istnieje na Discordzie.");
                        }

                        // Dzielimy zaszyfrowany plik na części
                        int chunkSize = 8 * 1024 * 1024; // 8 MB
                        int totalChunks = (int)Math.Ceiling((double)encryptedData.Length / chunkSize);

                        var tempFilePaths = new List<string>();

                        // Dzielimy zaszyfrowany plik na kawałki
                        for (int i = 0; i < totalChunks; i++)
                        {
                            var chunk = encryptedData.Skip(i * chunkSize).Take(chunkSize).ToArray();
                            var chunkFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(file.FileName)}_part{i}{Path.GetExtension(file.FileName)}");
                            System.IO.File.WriteAllBytes(chunkFilePath, chunk);
                            tempFilePaths.Add(chunkFilePath);
                        }

                        // Wysyłamy wszystkie części na Discord
                        foreach (var tempFilePath in tempFilePaths)
                        {
                            await _discordBot.SendFileAsync(tempFilePath);
                        }

                        // Usuwamy tymczasowe pliki
                        foreach (var tempFilePath in tempFilePaths)
                        {
                            System.IO.File.Delete(tempFilePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd podczas przesyłania pliku {file.FileName}: {ex.Message}");
                    return StatusCode(500, $"Wystąpił błąd podczas przesyłania pliku {file.FileName}: {ex.Message}");
                }
            }

            return Ok("Pliki zostały wysłane na Discord.");
        }
        private async Task<bool> FileExistsOnDiscord(string fileName)
        {
            var files = await _discordBot.GetFilesAsync();

            var discordFileNames = files.Select(f =>
            {
                var name = f.Filename;
                var partIndex = name.LastIndexOf("_part");
                if (partIndex >= 0)
                {
                    return name.Substring(0, partIndex) + Path.GetExtension(name);
                }
                return name;
            }).ToList();

            return discordFileNames.Any(n => n == fileName);
        }

        [HttpGet("Files")]
        public async Task<IActionResult> GetCloudFiles()
        {
            try
            {
                var aggregatedFiles = await _discordBot.GetAggregatedFilesAsync();
                return Ok(aggregatedFiles);
            }catch(Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                return StatusCode(500, $"Wystąpił błąd podczas pobierania plików: {ex.Message}");
            }
        }

        [HttpGet("Download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            try
            {
                var aggregatedFile = await _discordBot.GetFilesAsync(fileName);

                if (aggregatedFile == null)
                {
                    return NotFound("Plik nie znaleziony");
                }

                byte[] combinedBytes = await CombineFileParts(aggregatedFile.Urls);

                var decryptor = new Decryptor();

                using (var encryptedStream = new MemoryStream(combinedBytes))
                {
                    // Odszyfrowanie pliku
                    byte[] decryptedFile = decryptor.Decrypt(encryptedStream, this.pass);

                    // Zwrócenie odszyfrowanego pliku z odpowiednim typem MIME
                    return File(decryptedFile, "application/octet-stream", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Wystąpił błąd podczas pobierania pliku: {ex.Message}");
            }
        }
        [HttpDelete("Delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                // Wyszukaj wszystkie części pliku
                var files = await _discordBot.GetFilesAsync();

                // Filtrujemy pliki, które pasują do podanej nazwy (ignorując część "_partX")
                var matchingFiles = files.Where(f =>
                {
                    var name = f.Filename;
                    var partIndex = name.LastIndexOf("_part");
                    if (partIndex >= 0)
                    {
                        var baseFileName = name.Substring(0, partIndex) + Path.GetExtension(name);
                        return baseFileName == fileName;  // Dopasuj nazwę pliku bazowego
                    }
                    return f.Filename == fileName;  // Dopasuj dokładną nazwę
                }).ToList();

                if (!matchingFiles.Any())
                {
                    return NotFound($"Plik o nazwie '{fileName}' nie został znaleziony.");
                }

                // Usuwanie plików na Discordzie
                foreach (var file in matchingFiles)
                {
                    try
                    {
                        await _discordBot.DeleteFileAsync(file.Id); // Zakładając, że masz metodę DeleteFileAsync w DiscordBot
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas usuwania pliku {file.Filename}: {ex.Message}");
                        return StatusCode(500, $"Wystąpił błąd podczas usuwania pliku {file.Filename}: {ex.Message}");
                    }
                }

                // Usuwanie plików lokalnych (jeśli istnieją)
                var tempFilePaths = Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(fileName)}*");
                foreach (var tempFilePath in tempFilePaths)
                {
                    try
                    {
                        System.IO.File.Delete(tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas usuwania lokalnego pliku {tempFilePath}: {ex.Message}");
                        return StatusCode(500, $"Wystąpił błąd podczas usuwania lokalnego pliku {tempFilePath}: {ex.Message}");
                    }
                }

                return Ok($"Plik '{fileName}' oraz jego części zostały usunięte.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                return StatusCode(500, $"Wystąpił błąd podczas usuwania pliku: {ex.Message}");
            }
        }

        private async Task<byte[]> CombineFileParts(List<string> partUrls)
        {
            // Sortowanie według numerów w nazwach plików (_part0, _part1, _part2...)
            var sortedPartUrls = partUrls.OrderBy(url =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"_part(\d+)");
                return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
            }).ToList();

            using (var memoryStream = new MemoryStream())
            {
                foreach (var partUrl in sortedPartUrls)
                {
                    try
                    {
                        using (var partStream = await _httpClient.GetStreamAsync(partUrl))
                        {
                            await partStream.CopyToAsync(memoryStream);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Błąd pobierania fragmentu {partUrl}: {ex.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Wystąpił błąd podczas przetwarzania fragmentu {partUrl}: {ex.Message}");
                        throw;
                    }
                }

                return memoryStream.ToArray();
            }
        }

    }
}
