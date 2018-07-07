using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("A(y,TRUE)")));
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("E(y,TRUE)")));
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("~TRUE")));
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("TRUE")));
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("~~TRUE")));
            Console.WriteLine(FormulaCNF.QBFSAT(FormulaParser.parse("TRUE & ~TRUE")));
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
