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
            Timer timer = new Timer(OnTimerTick,script,0,33);
            Console.ReadKey();
        }

        private static void OnTimerTick(object state)
        {
            Script script = (Script)state;
            script.Execute("update");
        }
    }
}   
