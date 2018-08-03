﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTLSAT
{
    class SatisfiabilityTests
    {
        
        //Copied from https://stackoverflow.com/questions/7413612/how-to-limit-the-execution-time-of-a-function-in-c-sharp
        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
        {
            try
            {
                Task task = Task.Factory.StartNew(() => codeBlock());
                task.Wait(timeSpan);
                return task.IsCompleted;
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("TIMEOUT");
                throw ae.InnerExceptions[0];
            }
        }

        private static int timelimit = 5*60000;

        private static void AssertSat(string formulaString, bool expected)
        {
            bool Completed = ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(timelimit), () =>
            {
                FormulaNode formula = FormulaParser.parse(formulaString);
                Console.WriteLine("\nTEST: " + formulaString);
                var checker = new CTLSatisfiabilityChecker(formula);
                bool result = checker.check();
                Console.WriteLine("Done");
                if (result != expected)
                    throw new Exception("Wrong SAT value for " + formulaString);
            });
        }


        public static void run()
        {
            // TRUE
            AssertSat("TRUE", true);
            AssertSat("~TRUE", false);

            // Propositional formulas
            AssertSat("p", true);
            AssertSat("p & ~p", false);
            AssertSat("p | ~p", true);
            AssertSat("p & q", true);

            // EX and AX
            AssertSat("EX(p)", true);
            AssertSat("AX(p)", true);
            AssertSat("EX(p) & AX(~p)", false);
            AssertSat("EX(~TRUE)", false);
            AssertSat("p & AX(~p)", true);

            // EU
            AssertSat("EU(p,p)", true);
            AssertSat("~EU(p,p)", true);
            AssertSat("~EU(p,~p)", true);
            AssertSat("EU(~p,p)", true);
            AssertSat("EU(p,p) & ~p", false);
            AssertSat("EU(p,q) & ~p", true);
            AssertSat("EU(p,q) & ~q", true);
            AssertSat("EU(p,q) & ~p & ~q", false);

            // AU
            AssertSat("AU(p,p)", true);
            AssertSat("AU(p,q)", true);

            // AG
            AssertSat("AG(p)", true);
            AssertSat("AG(~TRUE)", false);
            AssertSat("~AG(TRUE)", false);

            // AF
            AssertSat("AF(p)", true);
            AssertSat("AF(EX(p))", true);

            // EF
            AssertSat("EF(p) & EF(q)", true);
            AssertSat("EF(p) & AG(~p)", false);

            // EG
            AssertSat("EG(p)", true);
            AssertSat("EG(p) & ~p", false);

            // ER
            AssertSat("ER(p,q)", true);
            AssertSat("ER(p,q) & ~p", true);
            AssertSat("ER(p,q) & ~p & ~q", false);

            // AR
            AssertSat("AR(p,p)", true);
            AssertSat("AR(~p,p)", true);
            AssertSat("AR(~p,p) & ~p", false);
            
        }

        public static void runLongTests()
        {
            run();
            AssertSat("AG(p) & AF(~p)", false);
            AssertSat("EX(p) & EX(~p)", true);
            AssertSat("AG(p) & EX(~p)", false);
            AssertSat("EF(p) & EF(~p)", true);
            AssertSat("EG(EX(p)) & ~p", true);
            AssertSat("AR(~TRUE,q) & EX(~q)", false);
            AssertSat("AR(~p,p) & EX(~p)", false);
        }
    }
}