using System;

namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //Console.WriteLine(FormulaNode.CreateTestFormula().ToString());
            Console.WriteLine(FormulaParser.parse("AG(EF(p))").ToString());
            Console.WriteLine(FormulaParser.parse("EF(p & ~q)").ToString());
            Console.WriteLine(FormulaParser.parse("AG(~EF(p))").ToString());
            Console.ReadLine();
        }
    }
}
