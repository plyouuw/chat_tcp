using System.Net;
using System.Net.Sockets;
using System.Text;
using static Toolbox;

namespace chat_tcp
{
    internal class Server
    {
        private class Client
        {
            public TcpClient TCP { get; }
            public Socket Socket { get; }
            public NetworkStream Stream { get; }

            public Client(TcpClient client)
            {
                TCP = client;
                Socket = client.Client;
                Stream = client.GetStream();
            }

            public bool Active()
            {
                if (TCP == null)
                    return false;
                try
                {
                    if (!Socket.Connected)
                        return false;
                    // Detect if client disconnected
                    if (Socket != null && Socket.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (Socket.Receive(buff, SocketFlags.Peek) == 0)
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
            public void Polaczenie()
            {
                byte[] buffer = new byte[4096];

                int received;
                string message;
                while (Active())
                {
                    // sprawdzenie, czy s¹ jakieœ dane do odczytu
                    if (Stream.DataAvailable)
                    {
                        // odczyt ze strumienia danych
                        try { received = Stream.Read(buffer, 0, buffer.Length); }
                        catch { break; }
                        Stream.Flush();
                        message = Encoding.UTF8.GetString(buffer, 0, received);
                        // odpowiedŸ
                        try
                        {
                            Stream.Write(Encoding.UTF8.GetBytes($"response: {IncomingHandler(message)}"));
                        }
                        catch
                        {
                            break;
                        }
                    }

                }
                if (Active()) TCP.Close();
                if (Console.CursorLeft > 0) Print('\n');
                Print("\nZakoñczono po³¹czenie z klientem\n");
            }

            private string IncomingHandler(string data)
            {
                string response;
                data = data.Trim();
                Print($"\n[Client] {data}");

                if (data == "exit" || data == "close" || data == "quit")
                {
                    if (Active()) TCP.Close();
                    response = "Exiting.";
                }
                else if (data == "date")
                {
                    response = DateTime.Now.ToString();
                }
                else response = data;

                return response;
            }
        }
#pragma warning disable IDE0044 // Dodaj modyfikator tylko do odczytu
        private TcpListener listener;
        private IPEndPoint end_point;
        private List<Client> clients = new();
#pragma warning restore IDE0044 // Dodaj modyfikator tylko do odczytu
        public Server(string IpAddress, int port)

        {
            end_point = new IPEndPoint(IPAddress.Parse(IpAddress), port);
            listener = new(end_point);
        }

        public void Start()
        {
            Console.Clear();
            Console.TreatControlCAsInput = true;
            try
            {
                listener.Start();
            }
            catch
            {
                PrintError("\nWyst¹pi³ problem z uruchomieniem serwera!\n");
                return;
            }
            
            Print($"Serwer {end_point.Address} uruchomiony, nas³uchujê na porcie {end_point.Port}...\n");

            Thread watek;
            Client client;
            ConsoleKeyInfo cki;
            while (true)
            {
                if(listener.Pending()) 
                {
                    client = new(listener.AcceptTcpClient());
                    watek = new(client.Polaczenie);
                    watek.Start();
                    if (watek.ThreadState == ThreadState.Running)
                    { 
                        lock (clients) clients.Add(client);
                        Print($"\nNawi¹zano po³¹czenie z klientem\n");
                    }
                }
                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Enter)
                    {
                        lock (clients)
                        {
                            foreach (Client c in clients)
                            {
                                if (!c.Active())
                                {
                                    clients.Remove(c);
                                    continue;
                                }
                                try
                                {
                                    c.Stream.Write(Encoding.UTF8.GetBytes("ping"));
                                }
                                catch
                                {
                                    break;
                                }
                            }
                        }
                    } 
                    else if (cki.Key == ConsoleKey.C && cki.Modifiers == ConsoleModifiers.Control)
                    {
                        lock (clients)
                        {
                            foreach (Client c in clients)
                            {
                                if (c.Active())
                                {
                                    c.TCP.Close();
                                }
                            }
                        }
                        break;
                    }
                }
            }

            Print("\nZakoñczono pracê serwera.\n");
        }
    }
}