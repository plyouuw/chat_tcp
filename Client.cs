using System.Net;
using System.Net.Sockets;
using System.Text;
using static Toolbox;

namespace chat_tcp {
    internal class Client
    {
        private string ip_address = "";
        private int port = 0;

        private static List<(string prefix, string message)> chat = new();

        private TcpClient? client;

        private readonly struct Prefix
        {
            public static readonly string Server = "[Server]";
            public static readonly string Client = "[Client]";
            public static readonly string Input = ">";
        }

        public Client(string ip_address, int port)
        {
            try
            {
                IPAddress.Parse(ip_address);
            }
            catch
            {
                PrintError("Wystąpił problem z ustanowieniem połączenia ze zdalnym serwerem (constructor: IPAddress.Parse exception)!\n");
                Czekaj();
                return;
            }
            this.ip_address = ip_address;
            this.port = port;
        }

        private bool Active()
        {
            if (client == null)
                return false;
            try
            {
                if (!client.Client.Connected)
                    return false;
                // Detect if client disconnected
                if (client.Client != null && client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        private string IncomingHandler(string data)
        {
            if (client == null)
            {
                PrintError("\nWystąpił problem z połączeniem z serwerem! (incominghandler: client is null)\n");
                return "";
            }
            string response;
            data = data.Trim();

            if (data == "exit" || data == "close" || data == "quit")
            {
                if (Active()) client.Close();
                response = "Exiting.";
            }
            else if (data == "date")
            {
                response = DateTime.Now.ToString();
            }
            else response = data;

            return response;
        }
        private static void PrintChat()
        {
            lock (chat)
            {
                if (chat.Count > 0)
                {
                    int temp_pos = 0;
                    for(int x = 0; x <= (int)(chat[chat.Count - 1].message.Length / (Console.BufferWidth - 2)); x++)
                    {
                        temp_pos = Console.CursorTop;
                        Console.SetCursorPosition(0, Console.CursorTop - x);
                        Console.Write("".PadRight(Console.BufferWidth));
                        Console.SetCursorPosition(0, temp_pos - x);
                    }
                    
                    Print($"{chat[chat.Count - 1].prefix} {chat[chat.Count - 1].message}\n");
                }
                Print("> ");
            }
        }
        private bool Receiver()
        {
            if (client == null)
            {
                PrintError("\nWystąpił problem z połączeniem z serwerem! (receiver: client is null)\n");
                return false;
            }

            byte[] buffer = new byte[4096];
            int received;
            string message, handled;
            if (client.GetStream().DataAvailable)
            {
                try
                {
                    received = client.GetStream().Read(buffer, 0, buffer.Length);
                }
                catch
                {
                    PrintError("\nWystąpił problem z połączeniem z serwerem! (receiver: stream.Read exception)\n");
                    return false;
                }
                message = Encoding.UTF8.GetString(buffer, 0, received);
                if ((handled = IncomingHandler(message)) == message)
                {
                    lock (chat)
                    {
                        chat.Add((Prefix.Server, message));
                    }
                }
                else
                {
                    try
                    {
                        client.GetStream().Write(Encoding.UTF8.GetBytes(handled));
                    }
                    catch
                    {
                        PrintError("\nWystąpił problem z połączeniem z serwerem! (receiver: stream.Write exception)\n");
                        return false;
                    }
                }
            }
            return true;
        }
        private void Transmit(string data)
        {
            if (client == null)
            {
                PrintError("\nWystąpił problem z połączeniem z serwerem! (transmit: client is null)\n");
                return;
            }

            try
            {
                client.GetStream().Write(Encoding.UTF8.GetBytes(data));
            }
            catch
            {
                PrintError("\nWystąpił problem z połączeniem z serwerem! (receiver: stream.Write exception)\n");
            }
        }
        public void Start()
        {
            Console.Clear();
            if (ip_address == "" || port < 0)
            {
                PrintError("Wystąpił problem z ustanowieniem połączenia ze zdalnym serwerem (start: endpoint is null)!\n");
                Czekaj();
                return;
            }
            try
            {
                client = new(ip_address, port);
            }
            catch 
            {
                PrintError($"Wystąpił problem z ustanowieniem połączenia ze zdalnym serwerem! {ip_address}:{port}\n");
                Czekaj();
                return;
            }


            PrintChat();

            int last_count = chat.Count;
            string input = "";
            ConsoleKeyInfo temp_cki;
            while (Active() && Receiver())
            {
                if (last_count != chat.Count)
                {
                    last_count = chat.Count;
                    PrintChat();
                }

                if (Console.KeyAvailable)
                {
                    temp_cki = Console.ReadKey(true);
                    if(temp_cki.Key == ConsoleKey.Enter)
                    {
                        if(input.Trim().Length > 0)
                        {
                            lock (chat)
                                chat.Add((Prefix.Client, input));
                            Transmit(input);
                            input = "";
                        } 
                        else
                        {
                            Console.CursorLeft = 0;
                            Print("".PadRight(Console.BufferWidth));
                            Console.CursorLeft = 0;
                            Print("> ");
                            continue;
                        }
                    }
                    else if(temp_cki.Key == ConsoleKey.Backspace)
                    {
                        if (input != "") 
                        {
                            input = input.Remove(input.Length - 1);
                            Console.CursorLeft--;
                            Print(' ');
                            Console.CursorLeft--;
                        }
                    }
                    else if(temp_cki.Key != ConsoleKey.Escape && temp_cki.Modifiers != ConsoleModifiers.Control)
                    {
                        input += temp_cki.KeyChar;
                        if (input.Trim().Length > 0)
                            Print(temp_cki.KeyChar);
                        else
                            input = "";
                        
                    }
                }
                
            }
            
            Console.Clear();
            Print("Zakończono połączenie.\n");


            Czekaj();
            return;
        }
    }
}