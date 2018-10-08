using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lab2BPiD.Models;
using Microsoft.AspNet.SignalR;

namespace Lab2BPiD.Hubs
{
    public class ChatHub : Hub
    {
        static List<User> Users = new List<User>();
        
        public void SendWelcomeMessage()
        {
            Clients.All.addMessage("server", "Hello world!");
        }

        // Отправка сообщений
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }

        // Подключение нового пользователя
        public void Connect(string userName)
        {
            var id = Context.ConnectionId;


            if (!Users.Any(x => x.ConnectionId == id))
            {
                Users.Add(new User { ConnectionId = id, Name = userName });

                // Посылаем сообщение текущему пользователю
                Clients.Caller.onConnected(id, userName, Users);

                // Посылаем сообщение всем пользователям, кроме текущего
                Clients.AllExcept(id).onNewUserConnected(id, userName);

                Clients.AllExcept(id).addMessage("server", "User <" + userName + "> entered this chat");
            }
        }

        // Отключение пользователя
        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = Users.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                Users.Remove(item);
                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.Name);
                Clients.AllExcept(id).addMessage("server", "User <" + item.Name + "> left this chat");
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}