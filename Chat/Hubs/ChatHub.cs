using Chat.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Chat.Hubs
{
    public interface IChatClient
    {
        public Task ReceiveMessage(string username, string message);    
    }
    public class ChatHub : Hub<IChatClient>
    {
        private readonly IDistributedCache _cache;
        public ChatHub(IDistributedCache cache){
            _cache = cache;
        }
        // user can join to chat
        public async Task JoinChat(UserConnection connection)
        {
            // обращается к группам
            await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatRoom);
            //for cache 
            //то есть. Добавляем в кеш данные, что мы что то писали и потом использовали в методе sendmessage чтобы не переподключаться
            var stringConnection = JsonSerializer.Serialize(connection);

            await _cache.SetStringAsync(Context.ConnectionId,stringConnection);
            //напрямую связь с фронтом
            await Clients.Group(connection.ChatRoom).ReceiveMessage("Admin", $"{connection.Username} присоеднилился к чату");
        }

        public async Task SendMessage(string message)
        {
            var stringConnection = await _cache.GetStringAsync(Context.ConnectionId);
            var connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);
            await Clients
                .Group(connection.ChatRoom).ReceiveMessage(connection.Username, message);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var stringConnection = await _cache.GetStringAsync(Context.ConnectionId);
            var connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);

            if(connection is not null)
            {
                await _cache.RemoveAsync(Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.ChatRoom);
            }
            await Clients.Group(connection.ChatRoom).ReceiveMessage("Admin", $"{connection.Username} вышел из чата");

            await base.OnDisconnectedAsync(exception);
        }
    }
}
