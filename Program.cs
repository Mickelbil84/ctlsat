using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var formula = FormulaParser.parse("AX(p&q)|AX(~(p|q))"); 
            Console.WriteLine(formula);
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
