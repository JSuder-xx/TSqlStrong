using System;
using System.Linq;
using System.Reflection;
using NSpec;
using NSpec.Domain;
using NSpec.Domain.Formatters;

namespace TSqlStrongSpecifications
{
    public class Startup
    {
        [STAThread]
        static public int Main(string[] args)
        {
            var tagOrClassName = String.Empty;

            var types = typeof(Startup).Assembly.GetTypes();
            var finder = new SpecFinder(types, "");

            var tagsFilter = new Tags().Parse(tagOrClassName);
            var builder = new ContextBuilder(finder, tagsFilter, new DefaultConventions());

            var runner = new ContextRunner(tagsFilter, new ConsoleFormatter(), false);
            var results = runner.Run(builder.Contexts().Build());

            System.Console.ReadLine();
            return (results.Failures().Count() > 0)
                ? -1
                : 0;
        }
    }
}
