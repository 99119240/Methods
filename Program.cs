using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ware
{
    class Program
    {
        static void Main(string[] args)
        {
            var debug = 0;
            var production = 1;

            if (debug == 1)
            {
                Console.WriteLine("Application is running in debug mode");
                // Call debug functions here
                // Stop loop
                Debug();
            }

            if (production == 1)
            {
                Console.WriteLine("Built for production");
                // Start production code here
                Console.WriteLine("Production yay!");
                Production();
            }
        }

        private static void Debug()
        {
            Console.ReadLine();
        }

        private static void Production()
        {
            Console.ReadLine();
        }

        private static void ProductionOutput()
        {
            // Log stuff here
        }
    }
}
