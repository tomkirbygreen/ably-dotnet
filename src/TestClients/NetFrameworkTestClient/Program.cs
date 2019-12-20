using System;
using System.Threading.Tasks;

namespace NetFrameworkTestClient
{
    internal class Program
    {
        private static string clientId = ".NetFramework Console -" + Guid.NewGuid().ToString().Split('-')[0];

        public static void Main(string[] args)
        {
            var ably = new AblyService("lNj80Q.iGyVcQ:2QKX7FFASfX-7H9H");
            ably.Init(clientId);

            var model = new MainViewModel(ably);
            Console.Read();
        }
    }
}