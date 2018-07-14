using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTLSAT
{
    class CTLUtils
    {
        /* Assumes the formula is in NNF form */
        public static ISet<FormulaNode> elementaryFormulas(FormulaNode formula)
        {
            HashSet<FormulaNode> result = new HashSet<FormulaNode>();
            switch (formula.GetLogicOperator())
            {
                case LogicOperator.AU:
                case LogicOperator.AR:
                    // AX(f) is elementary
                    FormulaNode ax_f = new FormulaNode(LogicOperator.AX);
                    ax_f.SetChildren(formula, null);
                    result.Add(ax_f);
                    result.Add(nnfNegate(ax_f));
                    result.UnionWith(elementaryFormulas(formula[0]));
                    result.UnionWith(elementaryFormulas(formula[1]));
                    break;
                case LogicOperator.EU:
                case LogicOperator.ER:
                    // EX(f) is elementary
                    FormulaNode ex_f = new FormulaNode(LogicOperator.EX);
                    ex_f.SetChildren(formula, null);
                    result.Add(ex_f);
                    result.Add(nnfNegate(ex_f));
                    result.UnionWith(elementaryFormulas(formula[0]));
                    result.UnionWith(elementaryFormulas(formula[1]));
                    break;
                case LogicOperator.EX:
                case LogicOperator.AX:
                    // the formula itself is elementary
                    result.Add(formula);
                    result.Add(nnfNegate(formula));
                    result.UnionWith(elementaryFormulas(formula[0]));
                    break;
                case LogicOperator.OR:
                case LogicOperator.AND:
                    result.UnionWith(elementaryFormulas(formula[0]));
                    result.UnionWith(elementaryFormulas(formula[1]));
                    break;
                case LogicOperator.NOT:
                    Debug.Assert(formula[0].GetLogicOperator() == LogicOperator.VAR,
                        "Input to elementaryFormulas must be in NNF form");
                    result.UnionWith(elementaryFormulas(formula[0]));
                    break;
                case LogicOperator.VAR:
                    if (formula.GetName() != FormulaNode.TRUE_LITERAL)
                    {
                        result.Add(formula);
                        result.Add(nnfNegate(formula));
                    }
                    break;
                default:
                    throw new Exception("Not implemented");

            }
            return result;
        }

        public static ISet<FormulaNode> positiveElementary(FormulaNode formula)
        {
            ISet<FormulaNode> allElementary = elementaryFormulas(formula);
            HashSet<FormulaNode> result = new HashSet<FormulaNode>();
            foreach (var f in allElementary)
            {
                if (f.GetLogicOperator() == LogicOperator.VAR ||
                    f.GetLogicOperator() == LogicOperator.EX)
                    result.Add(f);
            }
            return result;
        }

        public static FormulaNode nnfNegate(FormulaNode formula)
        {
            FormulaNode result = new FormulaNode(LogicOperator.NOT);
            result.SetChildren(formula, null);
            return result.NNF();
        }
    }
}
