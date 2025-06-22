using Discord;
using Discord.WebSocket;
using DiscordCloud.Core.Models;
using System.Net.Mail;

namespace DiscordCloud.Core.DiscordBotService
{
    public class DiscordBot
    {
        private readonly string Token = "YOUR DISCORD BOT TOKEN";
        private DiscordSocketClient _client;
        private readonly ulong channelId = 0; //Set Your OWN channelID
        public async Task ConnectAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            await _client.LoginAsync(TokenType.Bot, Token);
            await _client.StartAsync();
        }
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }
        public async Task SendFileAsync(string filePath)
        {
            var channel = _client.GetChannel(channelId) as ITextChannel;
            if (channel != null)
            {
                await channel.SendFileAsync(filePath);
            }
        }
        public async Task <List<IAttachment>> GetFilesAsync()
        {
            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            var attachments = messages.SelectMany(m => m.Attachments).ToList();
            return attachments;
        }

        public async Task<List<AggregatedFile>> GetAggregatedFilesAsync()
        {
            var attachments = await GetFilesAsync();
            var aggregatedFiles = new Dictionary<string, AggregatedFile>();

            foreach(var attachment in attachments)
            {
                var filename = attachment.Filename;
                var partIndex = filename.LastIndexOf("_part");
                string baseFileName="";

                if (partIndex >= 0)
                {
                    baseFileName = filename.Substring(0, partIndex) + System.IO.Path.GetExtension(filename);
                }
                else
                {
                    baseFileName=filename;
                }
                if (!aggregatedFiles.ContainsKey(baseFileName))
                {
                    aggregatedFiles[baseFileName] = new AggregatedFile
                    {
                        FileName = baseFileName,
                        CreationDate = attachment.CreatedAt.UtcDateTime,
                        FileSize = Math.Round(attachment.Size/(1024.0*1024.0),2),
                        Urls = new List<string> { attachment.Url }
                    };
                }
                else
                {
                    var existingFile = aggregatedFiles[baseFileName];
                    existingFile.FileSize += Math.Round(attachment.Size / (1024.0 * 1024.0),2);
                    existingFile.Urls.Add(attachment.Url);
                    if (attachment.CreatedAt.UtcDateTime > existingFile.CreationDate)
                    {
                        existingFile.CreationDate = attachment.CreatedAt.UtcDateTime;
                    }
                }
            }
            return aggregatedFiles.Values.ToList();
        }

        public async Task<AggregatedFile> GetFilesAsync(string fileName)
        {
            var aggregatedFiles = await GetAggregatedFilesAsync();
            return aggregatedFiles.FirstOrDefault(f => f.FileName == fileName);
        }

        public async Task DeleteFileAsync(ulong attachmentID)
        {
            var channel = await _client.GetChannelAsync(channelId) as ITextChannel;
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            var messageToDelete = messages.FirstOrDefault(m => m.Attachments.Any(a => a.Id == attachmentID));

            if (messageToDelete != null)
            {
                try
                {
                    await messageToDelete.DeleteAsync();
                }catch(Exception ex)
                {
                    Console.WriteLine($"Wystąpił błąd podczas usuwania pliku o ID {attachmentID}: {ex.Message}");
                }
            }
        }
    }
}
