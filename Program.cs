using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MangoScript
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Start();
        }
        public static void Start()
        {
            MangoInterpreter.Initialize();
            string code = File.ReadAllText(Environment.CurrentDirectory + "/code.mango");
            Script script = MangoInterpreter.LoadScript(code);
            Console.WriteLine("Script cargado: " + script.functions.Count + " funciones.");
            script.Execute("start");
            while (true)
            {
                script.Execute("update");
                Console.WriteLine("X = " + MangoInterpreter.GetVariable("x"));
            }
        }
    }
}   
