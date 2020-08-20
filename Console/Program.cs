using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var absoluteAssemblyPath = @"C:\repos\XunitInvalidCastException\Runner\bin\Debug\Runner.dll";
            Assembly assembly = Assembly.LoadFrom(absoluteAssemblyPath);
            Type jobType = assembly.GetType("Runner.Runner", throwOnError: true);
            dynamic job = Activator.CreateInstance(jobType);
            job.RunAsync().GetAwaiter().GetResult();
        }
    }
}
