using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lab2BPiD.Models;
using Microsoft.AspNet.SignalR;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
namespace Lab2BPiD.Hubs
{
    public class ChatHub : Hub
    {
        private const int KeyLength = 2048;
        static List<User> Users = new List<User>();
        UnicodeEncoding ByteConverter = new UnicodeEncoding();
        RSACryptoServiceProvider RSA;
        static Dictionary<string, RSAParameters> serverPrivateKeys = new Dictionary<string, RSAParameters>();
        static Dictionary<string, RSAParameters> clientPublicKeys = new Dictionary<string, RSAParameters>();

        //Decrypting message with server private key
        //Sending encrypted message to all clients
        public void Send(string name, string message)
        {
            byte[] textEncrypted = Convert.FromBase64String(message);
            RSA = new RSACryptoServiceProvider(2048);
            var a = Decrypt(textEncrypted, serverPrivateKeys[Context.ConnectionId]);
            var msgDecrypted = Encoding.UTF8.GetString(a);

            foreach (string id in clientPublicKeys.Keys)
            {
                Clients.Client(id).addMessage(name, EncodeWholeMessage(msgDecrypted, id));
            }
        }

        public void SendWelcomeMessage()
        {
            Clients.Client(Context.ConnectionId).addMessage("server", EncodeWholeMessage("Hello world!", Context.ConnectionId));
        }

        public string EncodeWholeMessage(string input, string id)
        {
            RSA = new RSACryptoServiceProvider(2048);
            List<string> messagesb64 = new List<string>();
            byte[] textDecoded = Encoding.UTF8.GetBytes(input);

            while (textDecoded.Any())
            {
                byte[] block = textDecoded.Take(245).ToArray();
                byte[] blockEncrypted = Encrypt(block, clientPublicKeys[id]);
                //messagesb64.Add(Convert.ToBase64String(blockEncrypted));
                messagesb64.Add(Convert.ToBase64String(blockEncrypted));
                textDecoded = textDecoded.Skip(245).ToArray();
            }

            string totalTextb64 = string.Join(Environment.NewLine, messagesb64);
            return totalTextb64;
        }

        // New client connection
        public void Connect(string userName)
        {
            var id = Context.ConnectionId;

            if (!Users.Any(x => x.ConnectionId == id))
            {
                serverPrivateKeys.Add(Context.ConnectionId, GenerateNewPrivateKey());
                var serverPublicKey = GeneratePublicKeyBase64(serverPrivateKeys[Context.ConnectionId]);

                Users.Add(new User { ConnectionId = id, Name = userName });

                Clients.Caller.onConnected(id, userName, Users);

                Clients.AllExcept(id).onNewUserConnected(id, userName);

                SendServerMessage("User <" + userName + "> entered this chat");

                Clients.Caller.sendPublicKey(JsonConvert.SerializeObject(new string[] { serverPublicKey[0], serverPublicKey[1] }));
            }
        }

        public void SendServerMessage(string message)
        {
            foreach (string id in clientPublicKeys.Keys)
            {
                Clients.Client(id).addMessage("server", EncodeWholeMessage(message, id));
            }
        }

        // Disconnect client
        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = Users.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                Users.Remove(item);
                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(id, item.Name);

                SendServerMessage("User <" + item.Name + "> left this chat");
            }

            return base.OnDisconnected(stopCalled);
        }

        public void GetPublicKey(string e, string n)
        {
            clientPublicKeys.Add(Context.ConnectionId, new RSAParameters
            {
                Exponent = Convert.FromBase64String("AQAB"),
                Modulus = Convert.FromBase64String(n)
            });
        }

        private RSAParameters GenerateNewPrivateKey()
        {
            RSA = new RSACryptoServiceProvider(KeyLength);
            var serverPrivateKey = RSA.ExportParameters(true);

            return serverPrivateKey;
        }

        private string[] GeneratePublicKeyBase64(RSAParameters pbk)
        {
            string E = Convert.ToBase64String(pbk.Exponent);
            string N = Convert.ToBase64String(pbk.Modulus);

            return new[] { E, N };
        }

        private byte[] Encrypt(byte[] text, RSAParameters publicKeys)
        {
            try
            {
                RSA = new RSACryptoServiceProvider(2048);
                RSA.ImportParameters(publicKeys);
                byte[] textEncrypted = RSA.Encrypt(text, false);

                return textEncrypted;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null;
        }

        private byte[] Decrypt(byte[] textEncrypted, RSAParameters privateKey)
        {
            try
            {
                RSA.ImportParameters(privateKey);
                byte[] textDecrypted = RSA.Decrypt(textEncrypted, false);

                return textDecrypted;
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return null;
        }
    }
}