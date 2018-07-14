using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var checker = new CTLSatisfiabilityChecker(FormulaParser.parse("AX(p&q)")); 
            Console.WriteLine(checker.check());
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
