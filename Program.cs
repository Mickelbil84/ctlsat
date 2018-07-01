using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            CTLSatisfiabilityChecker checker = new CTLSatisfiabilityChecker(FormulaParser.parse("~p&EX(p)"));
            checker.check();
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
