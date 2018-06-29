using System;
using System.Collections.Generic;

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

            Console.WriteLine();

            // Test the formula from wikipedia:
            // ((p|q)&r) -> (~s)
            FormulaNode f1 = new FormulaNode(LogicOperator.IMP);
            FormulaNode f2 = new FormulaNode(LogicOperator.AND);
            FormulaNode f3 = new FormulaNode(LogicOperator.OR);
            f3.SetChildren(new FormulaNode("p"), new FormulaNode("q"));
            f2.SetChildren(f3, new FormulaNode("r"));
            FormulaNode f4 = new FormulaNode(LogicOperator.NOT);
            f4.SetChildren(new FormulaNode("s"), null);
            f1.SetChildren(f2, f4);
            Console.WriteLine(f1);
            FormulaCNF.QBCNFormula cnf = FormulaCNF.ConvertToCNF(f1);
            Console.WriteLine(cnf);

            Console.WriteLine();

            FormulaNode g1 = new FormulaNode(LogicOperator.ALL, "p");
            FormulaNode g2 = new FormulaNode(LogicOperator.EXISTS, "q");
            FormulaNode g3 = new FormulaNode(LogicOperator.EXISTS, "oops");
            g1.SetChildren(g2, null);
            g2.SetChildren(g3, null);
            g3.SetChildren(f1, null);
            Console.WriteLine(g1);
            cnf = FormulaCNF.ConvertToCNF(g1);
            Console.WriteLine(cnf);

        }
    }
}
