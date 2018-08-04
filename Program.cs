using System;
using System.Threading;
using System.Diagnostics;

namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if (args[0] == "$test")
                    SatisfiabilityTests.run();
                else if (args[0] == "$longtest")
                    SatisfiabilityTests.runLongTests();
                else
                    RunFormula(args[0]);
                return;
            }

            TitleScreen();
            while(true)
            {
                Console.WriteLine("Please enter a formula: (to quit, enter 'quit')");
                string input = Console.ReadLine();
                if (input == "quit") break;

                threadFormula = input;
                Thread ctlthread = new Thread(RunFormulaThread);
                ctlthread.Start();
                while(ctlthread.IsAlive)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        ctlthread.Abort();
                        QBFSolver.AbortSolver();
                        Console.WriteLine("ABORTED");
                        PrintLine();
                    }
                }
            }

        }

        private const int lineSize = 17;
        private const char lineChar = '-';

        public static void PrintLine()
        {
            for (int i = 0; i < lineSize; i++)
                Console.Write(lineChar);
            Console.WriteLine();
        }

        public static string ConvSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }

        public static string threadFormula;
        public static void RunFormulaThread()
        {
            RunFormula(threadFormula);
        }

        public static void RunFormula(string formula)
        {
            PrintLine();
            Console.WriteLine("formula: " + formula);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            CTLSatisfiabilityChecker checker = new CTLSatisfiabilityChecker(FormulaParser.parse(formula));
            bool res = checker.check();
            stopwatch.Stop();
            double time = ((double)stopwatch.ElapsedMilliseconds / 1000.0);

            Console.WriteLine("iterations: " + checker.Iterations);
            Console.WriteLine("time: " + time);
            Console.WriteLine("result: " + ConvSAT(res));

            PrintLine();
        }

        public static void TitleScreen()
        {
            Console.WriteLine("****************************************");
            Console.WriteLine("*\t\tCTLSAT");
            Console.WriteLine("* Written by M. Bilevich and A. Zeitak");
            Console.WriteLine("****************************************");
        }

    }
}
