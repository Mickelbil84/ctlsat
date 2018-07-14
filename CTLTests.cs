using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace CTLSAT
{
    /*
     * A class for various CTL Tests and debugs
     */
    public class CTLTests
    {
        private struct TestInfo
        {
            public bool result;
            public double time;
        }

        private delegate FormulaNode TestFormula(int param);
        private delegate bool TestDelegate(TestFormula formula, int param);

        private static string log = "";

        private static bool TestSAT(TestFormula test, int param)
        {
            CTLSatisfiabilityChecker checker = new CTLSatisfiabilityChecker(test(param));
            return checker.check();
        }

        private static bool TestValidity(TestFormula test, int param)
        {
            FormulaNode formula = new FormulaNode(LogicOperator.NOT);
            formula.SetChildren(test(param), null);
            CTLSatisfiabilityChecker checker = new CTLSatisfiabilityChecker(formula);
            return !checker.check();
        }

        private static TestInfo RunTest(TestDelegate test, TestFormula formula, int param, int num = 100)
        {
            TestInfo res = new TestInfo
            {
                result = true
            };
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            for (int i = 0; i < num; i++)
            {
                try 
                {
                    if (!test(formula, param))
                    {
                        res.result = false;
                        break;
                    }
                }
                catch (Exception e)
                {
                    res.result = false;
                    log += e.Message;
                    log += "\n";
                    break;
                }

            }
            stopwatch.Stop();

            res.time = ((double)stopwatch.ElapsedMilliseconds / 1000.0) / num;
            return res;
        }

        public static FormulaNode TestInduction(int n)
        {
            FormulaNode res = new FormulaNode(LogicOperator.IMP);

            FormulaNode rhs = new FormulaNode(LogicOperator.AG);
            rhs.SetChildren(new FormulaNode(LogicOperator.AF), null);
            rhs[0].SetChildren(new FormulaNode("p0"), null);

            List<FormulaNode> terms = new List<FormulaNode>();
            terms.Add(new FormulaNode("p0"));

            for (int i = 0; i < n; i++)
            {
                FormulaNode ag = new FormulaNode(LogicOperator.AG);
                FormulaNode ax = new FormulaNode(LogicOperator.AX);
                ax.SetChildren(new FormulaNode("p" + ((i+1) % n)), null);
                FormulaNode imp = new FormulaNode(LogicOperator.IMP);
                imp.SetChildren(new FormulaNode("p" + i), ax);
                ag.SetChildren(imp, null);
                terms.Add(ag);
            }

            FormulaNode lhs = CTLSatisfiabilityChecker.joinTerms(LogicOperator.AND, terms);
            res.SetChildren(lhs, rhs);

            return res;
        }

        public static FormulaNode TestPrecede(int n)
        {
            FormulaNode res = new FormulaNode(LogicOperator.IMP);

            FormulaNode rhs = new FormulaNode(LogicOperator.AF);
            rhs.SetChildren(new FormulaNode("p0"), null);

            List<FormulaNode> terms = new List<FormulaNode>();
            terms.Add(new FormulaNode(LogicOperator.AF, new FormulaNode("p"+n), null));

            for (int i = 0; i <= n; i++)
                terms.Add(new FormulaNode(LogicOperator.NOT, new FormulaNode("p" + i), null));

            for (int i = 0; i < n; i++)
            {
                FormulaNode ag = new FormulaNode(LogicOperator.AG);
                FormulaNode ax = new FormulaNode(LogicOperator.AX);
                ax.SetChildren(new FormulaNode(LogicOperator.NOT, new FormulaNode("p" + (i+1)), null), null);
                FormulaNode imp = new FormulaNode(LogicOperator.IMP);
                imp.SetChildren(new FormulaNode(LogicOperator.NOT, new FormulaNode("p" + i), null), ax);
                ag.SetChildren(imp, null);
                terms.Add(ag);
            }

            FormulaNode lhs = CTLSatisfiabilityChecker.joinTerms(LogicOperator.AND, terms);
            res.SetChildren(lhs, rhs);

            return res;
        }

        public static FormulaNode TestFair(int n)
        {
            FormulaNode res = new FormulaNode(LogicOperator.IMP);

            FormulaNode rhs = new FormulaNode(LogicOperator.AG);
            rhs.SetChildren(new FormulaNode(LogicOperator.AF), null);
            rhs[0].SetChildren(new FormulaNode("p"+(n-1)), null);

            List<FormulaNode> terms = new List<FormulaNode>();
            terms.Add(new FormulaNode(LogicOperator.AG, new FormulaNode(LogicOperator.AF, new FormulaNode("p0"), null), null));

            for (int i = 0; i < n; i++)
            {
                FormulaNode ag = new FormulaNode(LogicOperator.AG);
                FormulaNode ax = new FormulaNode(LogicOperator.AX);
                ax.SetChildren(new FormulaNode(LogicOperator.AF, new FormulaNode("p" + ((i + 1) % n)), null), null);
                FormulaNode imp = new FormulaNode(LogicOperator.IMP);
                imp.SetChildren(new FormulaNode("p" + i), ax);
                ag.SetChildren(imp, null);
                terms.Add(ag);
            }

            FormulaNode lhs = CTLSatisfiabilityChecker.joinTerms(LogicOperator.AND, terms);
            res.SetChildren(lhs, rhs);

            return res;
        }

        public static FormulaNode TestNoBase(int n)
        {
            FormulaNode res = new FormulaNode(LogicOperator.IMP);

            FormulaNode rhs = new FormulaNode(LogicOperator.AG);
            rhs.SetChildren(new FormulaNode(LogicOperator.AF), null);
            rhs[0].SetChildren(new FormulaNode("p0"), null);

            List<FormulaNode> terms = new List<FormulaNode>();

            for (int i = 0; i < n; i++)
            {
                FormulaNode ag = new FormulaNode(LogicOperator.AG);
                FormulaNode ax = new FormulaNode(LogicOperator.AX);
                ax.SetChildren(new FormulaNode("p" + ((i + 1) % n)), null);
                FormulaNode imp = new FormulaNode(LogicOperator.IMP);
                imp.SetChildren(new FormulaNode("p" + i), ax);
                ag.SetChildren(imp, null);
                terms.Add(ag);
            }

            FormulaNode lhs = CTLSatisfiabilityChecker.joinTerms(LogicOperator.AND, terms);
            res.SetChildren(lhs, rhs);

            return new FormulaNode(LogicOperator.NOT, res, null);
        }

        private static void RunTestParams(string name, TestDelegate test, TestFormula formula, int[] prms, int num)
        {
            TestInfo res;

            for (int i = 0; i < prms.Length; i++)
            {
                res = RunTest(test, formula, prms[i], num);
                Console.Write(name + "_" + prms[i] + ":\t" + res.time);
                if (!res.result) Console.Write(" (FAILED)");
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        // Run the same test as in the article: https://link.springer.com/content/pdf/10.1007/978-3-540-31980-1_15.pdf
        // (All rights are reserved for the owners)
        public static void TestArticle(int num = 100)
        {
            //Run induction
            int[] indc_params = { 16, 20, 24, 28 };
            RunTestParams("induction", TestValidity, TestInduction, indc_params, num);

            //Run precede
            int[] prec_params = { 16, 32, 64, 128 };
            RunTestParams("precede", TestValidity, TestPrecede, prec_params, num);

            //Run fair
            int[] fair_params = { 8, 16, 32, 64, 128 };
            RunTestParams("fair", TestValidity, TestFair, fair_params, num);

            //Run nobase
            int[] nbse_params = { 16, 20, 24, 28 };
            RunTestParams("nobase", TestSAT, TestNoBase, nbse_params, num);
        }

        public static void ShowLog()
        {
            Console.WriteLine(log);
        }

    }
}
