using DDBot.DependencyInjection;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot
{
    public class Admin
    {
        static void Main(string[] args)
        {
            var container = Container.For<ConsoleRegistry>();

            var app = container.GetInstance<Admin>();
            app.Run().GetAwaiter().GetResult();
        }

        public async Task Run()
        {
            do
            {
                Console.WriteLine("Type 'q' to terminate");
                string input = Console.ReadLine();
                switch (input) {
                    case "q":
                        return;
                    default:
                        break;
                }
            } while (true);
        }
    }
}
