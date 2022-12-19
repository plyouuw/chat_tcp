using static Toolbox;

namespace chat_tcp
{
    internal class Program
    {
        public static void Main()
        {
            string ip = "127.0.0.1";
            int port = 8888;
            Dictionary<int, string> menu_list = new()
            {
                { 1, "Klient" },
                { 2, "Serwer" },
                { 0, "Wyjście z programu" }
            };
            Menu menu = new(Menu.Theme.WhiteArrow, menu_list, "Wybierz tryb działania:");
            int choice = menu.ReadChoice();
            if (choice == 1)
            {
                Client client = new(ip, port);
                client.Start();
            }
            else if (choice == 2)
            {
                Server serwer = new(ip, port);
                serwer.Start();
            }
            else return; 
        }
    }
}