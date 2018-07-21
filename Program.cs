using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //CTLSatisfiabilityChecker checker = new CTLSatisfiabilityChecker(FormulaParser.parse("AF(p)&EX(q)"));
            //Console.WriteLine(checker.check());

            CTLTests.TestArticle(1);
            //CTLTests.ShowLog();
            Console.ReadKey();
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
