using System;
using System.Collections.Generic;

namespace CTLSAT
{
    /* Represents a set of boolean variables, one for each positive elementary
     * formula, whose values together represent a state in the Hintikka structure
     */
    class SymbolicState
    {
        readonly IDictionary<FormulaNode, string> elementaryNames = new Dictionary<FormulaNode, string>();
        readonly IDictionary<string, FormulaNode> nameToElementary = new Dictionary<string, FormulaNode>();

        public SymbolicState(ISet<FormulaNode> positiveElementary, string prefix)
        {
            int i = 0;
            if (prefix.Contains("_"))
                throw new Exception("Underscores in prefix can cause name collisions between different states");
            foreach (var e in positiveElementary)
            {
                elementaryNames[e] = prefix + "_" + i.ToString();
                nameToElementary[prefix + i.ToString()] = e;
                i++;
            }
        }

        /* Build a propositional formula representing the value of the given CTL
         * formula in this state
         */
        public FormulaNode valueOf(FormulaNode formula)
        {
            FormulaNode result, er;
            switch (formula.GetLogicOperator())
            {
                case LogicOperator.VAR:
                case LogicOperator.EX:
                    // literals and EX(...) formulas are considered positive elementary,
                    // and so should correspond directly to a propositional variable
                    if (formula.GetLogicOperator() == LogicOperator.VAR &&
                        formula.GetName() == FormulaNode.TRUE_LITERAL)
                        return new FormulaNode(FormulaNode.TRUE_LITERAL);
                    return new FormulaNode(elementaryNames[formula]);

                case LogicOperator.AX:
                    FormulaNode notBody = CTLUtils.nnfNegate(formula[0]);
                    FormulaNode ex = new FormulaNode(LogicOperator.EX, notBody, null);
                    FormulaNode exValue = valueOf(ex);
                    return new FormulaNode(LogicOperator.NOT, exValue, null);

                case LogicOperator.EU:
                    result = valueOf(new FormulaNode(LogicOperator.EX, formula, null));
                    result = new FormulaNode(LogicOperator.AND, valueOf(formula[0]), result);
                    result = new FormulaNode(LogicOperator.OR, valueOf(formula[1]), result);
                    return result;

                case LogicOperator.AU:
                    er = new FormulaNode(LogicOperator.ER,
                                                    CTLUtils.nnfNegate(formula[0]),
                                                    CTLUtils.nnfNegate(formula[1]));
                    result = valueOf(new FormulaNode(LogicOperator.EX, er, null));
                    result = new FormulaNode(LogicOperator.NOT, result, null);
                    result = new FormulaNode(LogicOperator.AND, valueOf(formula[0]), result);
                    result = new FormulaNode(LogicOperator.OR, valueOf(formula[1]), result);
                    return result;

                case LogicOperator.ER:
                    result = valueOf(new FormulaNode(LogicOperator.EX, formula, null));
                    result = new FormulaNode(LogicOperator.OR, valueOf(formula[0]), result);
                    result = new FormulaNode(LogicOperator.AND, valueOf(formula[1]), result);
                    return result;

                case LogicOperator.AR:
                    er = new FormulaNode(LogicOperator.EU,
                                                    CTLUtils.nnfNegate(formula[0]),
                                                    CTLUtils.nnfNegate(formula[1]));
                    result = valueOf(new FormulaNode(LogicOperator.EX, er, null));
                    result = new FormulaNode(LogicOperator.NOT, result, null);
                    result = new FormulaNode(LogicOperator.OR, valueOf(formula[0]), result);
                    result = new FormulaNode(LogicOperator.AND, valueOf(formula[1]), result);
                    return result;

                case LogicOperator.AND:
                case LogicOperator.OR:
                    return new FormulaNode(formula.GetLogicOperator(), valueOf(formula[0]), valueOf(formula[1]));

                case LogicOperator.NOT:
                    FormulaNode bodyVar;
                    if (formula[0].GetLogicOperator() == LogicOperator.VAR &&
                        formula[0].GetName() == FormulaNode.TRUE_LITERAL)
                        bodyVar = new FormulaNode(FormulaNode.TRUE_LITERAL);
                    else if (!elementaryNames.ContainsKey(formula[0]))
                        throw new Exception("Argument to SymbolicState.valueOf must be contained in the closure, and in NNF form.");
                    else
                        bodyVar = new FormulaNode(elementaryNames[formula[0]]);
                    return new FormulaNode(LogicOperator.NOT, bodyVar, null);

                default:
                    throw new NotImplementedException();
            }
        }

        /* Replace the variables describing the state <from> with the ones that describe
         * state <to>
         */
        public static FormulaNode substitute(FormulaNode formula, SymbolicState from, SymbolicState to)
        {
            if (formula.GetLogicOperator() == LogicOperator.VAR)
            {
                if (from.nameToElementary.Keys.Contains(formula.GetName()))
                {
                    // this variable represents an elementary formula
                    var elementary = from.nameToElementary[formula.GetName()];
                    return new FormulaNode(to.elementaryNames[elementary]);
                }
                else
                {
                    return new FormulaNode(formula.GetName());
                }
            }

            if (formula.GetLogicOperator() == LogicOperator.ALL ||
                formula.GetLogicOperator() == LogicOperator.EXISTS)
            {
                FormulaNode result = new FormulaNode(formula.GetLogicOperator());
                if (from.nameToElementary.Keys.Contains(formula.GetName()))
                {
                    // this variable represents an elementary formula
                    var elementary = from.nameToElementary[formula.GetName()];
                    result.SetName(to.elementaryNames[elementary]);
                }
                else
                {
                    result.SetName(formula.GetName());
                }
                result.SetChildren(substitute(formula[0], from, to), null);
                return result;
            }

            FormulaNode res = new FormulaNode(formula.GetLogicOperator());

            FormulaNode left = substitute(formula[0], from, to);
            FormulaNode right = null;
            if (formula[1] != null)
                right = substitute(formula[1], from, to);
            res.SetChildren(left, right);
            return res;
        }

        public FormulaNode quantify(LogicOperator quantifier, FormulaNode formula)
        {
            FormulaNode result = formula;
            foreach (string name in elementaryNames.Values)
            {
                result = new FormulaNode(quantifier, result, null);
                result.SetName(name);
            }
            return result;
        }
    }
}
