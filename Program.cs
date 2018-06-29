using System;

namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            //Console.WriteLine(FormulaNode.CreateTestFormula().ToString());
            Console.WriteLine(FormulaParser.parse("AG(EF(p))").ToString());
            Console.WriteLine(FormulaParser.parse("~AU(p,q)").ToString());
            Console.WriteLine(FormulaParser.parse("EF(p & ~q)").ToString());
            Console.WriteLine(FormulaParser.parse("AG(~EF(p))").ToString());

            FormulaNode a = FormulaParser.parse("~AG(EF(p))");
            FormulaNode b = FormulaParser.parse("~EF(p & ~q)");
            FormulaNode c = FormulaParser.parse("~AG(~EF(p))");
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);

            Console.WriteLine();

            foreach (string s in a.GetVariables())
                Console.Write(s + " ");
            Console.WriteLine();
            foreach (string s in b.GetVariables())
                Console.Write(s + " ");
            Console.WriteLine();
            foreach (string s in c.GetVariables())
                Console.Write(s + " ");
            Console.WriteLine();

            Console.WriteLine();

            Console.WriteLine(b.Substitute("p", "w").Substitute("q", "p"));

            string str = c.UniqueVariable();
            FormulaNode d = c.Substitute("p", str);
            Console.WriteLine(str);
            Console.WriteLine(d);
            Console.WriteLine(d.UniqueVariable());

            Console.WriteLine();

            Console.WriteLine(a.NNF());
            Console.WriteLine(b.NNF());
            Console.WriteLine(c.NNF());

            Console.WriteLine();

            // *z(*x(!y(x&y))) | *y!z(y)
            FormulaNode d1 = new FormulaNode(LogicOperator.OR);
            FormulaNode d1_5 = new FormulaNode(LogicOperator.ALL, "z");
            FormulaNode d2 = new FormulaNode(LogicOperator.ALL, "x");
            FormulaNode d3 = new FormulaNode(LogicOperator.EXISTS, "y");
            FormulaNode d4 = new FormulaNode(LogicOperator.AND);
            d4.SetChildren(new FormulaNode("x"), new FormulaNode("y"));
            d3.SetChildren(d4, null);
            d2.SetChildren(d3, null);
            d1_5.SetChildren(d2, null);
            FormulaNode d5 = new FormulaNode(LogicOperator.ALL, "y");
            FormulaNode d6 = new FormulaNode(LogicOperator.EXISTS, "z");
            d6.SetChildren(new FormulaNode("y"), null);
            d5.SetChildren(d6, null);
            d1.SetChildren(d1_5, d5);

            Console.WriteLine(d1);
            Console.WriteLine(d1.PNF());
            Console.WriteLine(d1.PNF().GetPropositional());

            Console.WriteLine();

            // (*x(x)) -> (x & y)
            // EXPECTED:
            //  ~(*xx) | (x & y)
            // !x(~x) | (x & y)
            // !p0(~p0) | (x & y)
            // !p0 (~p0 | x & y)
            // UPDATE: Well, at least THAT probably works :)
            FormulaNode e1 = new FormulaNode(LogicOperator.IMP);
            FormulaNode e2 = new FormulaNode(LogicOperator.ALL, "x");
            e2.SetChildren(new FormulaNode("x"), null);
            FormulaNode e3 = new FormulaNode(LogicOperator.AND);
            e3.SetChildren(new FormulaNode("x"), new FormulaNode("y"));
            e1.SetChildren(e2, e3);

            Console.WriteLine(e1);
            Console.WriteLine(e1.PNF());
            Console.WriteLine(e1.PNF().GetPropositional());

        }
    }
}
