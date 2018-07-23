using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDBot.DependencyInjection
{
    public class ConsoleRegistry : Registry
    {
        public ConsoleRegistry()
        {
            Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
            });
            // requires explicit registration; doesn't follow convention
            For<ILog>().Use<ConsoleLogger>();
        }
    }

    public interface IWriter
    {
        void WriteLine(string output);
    }

    // will be automatically wired up by default convention
    public class Writer : IWriter
    {
        public void WriteLine(string output)
        {
            Console.WriteLine(output);
        }
    }

    public interface ILog
    {
        void Info(string message);
    }

    public class ConsoleLogger : ILog
    {
        public void Info(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = color;
        }
    }
}
