using DDBot.DependencyInjection;
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

        }
    }
}
