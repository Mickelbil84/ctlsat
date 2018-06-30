using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var el = CTLUtils.elementaryFormulas(FormulaParser.parse("AU(x|~x, p)&EX(q)"));
            foreach (var f in el)
                Console.WriteLine(f);
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
