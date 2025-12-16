using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Security.Claims;
using Tokki.Application.IServices; 
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly IMongoCollection<LiveChatMessage> _chatCollection;

        public ChatService(IConfiguration config)
        {
            var connectionString = config.GetConnectionString("MongoDbConnection");
            var databaseName = config["MongoDbSettings:DatabaseName"];
            var collectionName = config["MongoDbSettings:CollectionName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _chatCollection = database.GetCollection<LiveChatMessage>(collectionName);
        }

        public async Task CreateMessageAsync(LiveChatMessage message)
        {
            await _chatCollection.InsertOneAsync(message);
        }
        public async Task<List<LiveChatMessage>> GetHistoryAsync(string roomId)
        {
            return await _chatCollection.Find(x => x.RoomId == roomId)
                                        .SortByDescending(x => x.CreatedAt)
                                        .Limit(50)
                                        .ToListAsync();
        }
    }
}