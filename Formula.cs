using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace CTLSAT
{
    /*
     * An enumeration for all different logic
     * operators available in CTL
     */
    public enum LogicOperator
    {
        EXISTS, ALL,
        AND, OR, NOT, IMP,
        EF, AF, EG, AG,
        EX, AX, EU, AU, ER, AR,
        VAR,
        COMMA  // This is only used internally by the parsing logic
    }

    /*
     * A class that represents a single node in the 
     * derivation tree of a CTL formula
     */
    public class FormulaNode
    {
        private FormulaNode[] childNodes = new FormulaNode[2];
        private LogicOperator logicOp;
        private string name;

        public FormulaNode() {}
        public FormulaNode(string name)
        {
            this.name = name;
            this.logicOp = LogicOperator.VAR;
        }
        public FormulaNode(LogicOperator logicOp)
        {
            this.name = "";
            this.logicOp = logicOp;
        }
        public FormulaNode(LogicOperator logicOp, string name)
        {
            //Useful for existential operators 
            //e.g. Ay(...) -> forall y. (...)
            this.logicOp = logicOp;
            this.name = name;
        }

        public FormulaNode this[int key]
        {
            get
            {
                return childNodes[key];
            }
        }

        public void SetLogicOperator(LogicOperator logicOp)
        {
            this.logicOp = logicOp;
        }

        public void SetVariable(string name)
        {
            this.name = name;
            this.logicOp = LogicOperator.VAR;
        }

        public void SetChildren(FormulaNode left, FormulaNode right)
        {
            this.childNodes[0] = left;
            this.childNodes[1] = right;
        }

        public override string ToString() 
        {
            if (this.logicOp == LogicOperator.VAR)
                return this.name;

            string opstring = this.logicOp.ToString();

            if (this.logicOp == LogicOperator.EXISTS ||
                this.logicOp == LogicOperator.ALL) 
                opstring += this.name;

            if (this.childNodes[1] == null)
                return opstring + "(" + this.childNodes[0] + ")";

            string result = "";
            result += this.childNodes[0].ToString();
            result += " " + opstring + " ";
            result += this.childNodes[1].ToString();
            return result;
        }

        public static FormulaNode CreateTestFormula()
        {
            FormulaNode result = new FormulaNode(LogicOperator.EX);
            result.SetChildren(null, new FormulaNode("b"));

            result = new FormulaNode(LogicOperator.EU);
            FormulaNode a = new FormulaNode(LogicOperator.AG);
            a.SetChildren(new FormulaNode("p"), null);

            result.SetChildren(a, new FormulaNode("q"));
            return result;
        }

        // Return the set of all variables that appear in the formula
        public ISet<string> GetVariables()
        {
            ISet<string> res = new HashSet<string>();

            if (this.logicOp == LogicOperator.VAR)
            {
                res.Add(this.name);
                return res;
            }

            if (this.logicOp == LogicOperator.EXISTS ||
                this.logicOp == LogicOperator.ALL)
                res.Add(this.name);

            for (int i = 0; i < 2; i++)
                if (this.childNodes[i] != null)
                    res.UnionWith(this.childNodes[i].GetVariables());

            return res;
        }

        // Change f[x] to f[x/x']
        public FormulaNode Substitute(string original, string replacement)
        {
            if (this.logicOp == LogicOperator.VAR)
            {
                if (this.name == original)
                    return new FormulaNode(replacement);
                else
                    return new FormulaNode(this.name);
            }

            FormulaNode res = new FormulaNode(this.logicOp, this.name);

            if ((this.logicOp == LogicOperator.EXISTS ||
                this.logicOp == LogicOperator.ALL) &&
                (this.name == original))
                res.name = replacement;

            FormulaNode left = null, right = null;
            if (this.childNodes[0] != null)
                left = this.childNodes[0].Substitute(original, replacement);
            if (this.childNodes[1] != null)
                right = this.childNodes[1].Substitute(original, replacement);
            res.SetChildren(left, right);
            return res;
        }

        // Return a new variable that doesn't appear in the formula
        public string UniqueVariable()
        {
            ISet<string> variables = this.GetVariables();
            string res = "";
            int index = 0;

            while (true)
            {
                res = "p" + index.ToString();
                if (!variables.Contains(res))
                    break;
                index++;
            }

            return res;
        }

        // Transfer the node to NNF 
        public FormulaNode NNF()
        {
            if (this.logicOp == LogicOperator.VAR)
                return new FormulaNode(this.name);

            FormulaNode res = null, left = null, right = null;

            if (this.logicOp != LogicOperator.NOT)
            {
                res = new FormulaNode(this.logicOp, this.name);
                if (this.childNodes[0] != null)
                    left = this.childNodes[0].NNF();
                if (this.childNodes[1] != null)
                    right = this.childNodes[1].NNF();
                res.SetChildren(left, right);
                return res;
                
            }

            // If we reached here, our node is "~" and hence we should 
            // rearrange the formula tree
            // We note that a "~" should be unary and hence it only
            // has a left child

            switch (this.childNodes[0].logicOp)
            {
                case LogicOperator.EXISTS:
                    res = new FormulaNode(LogicOperator.ALL, this.childNodes[0].name);
                    break;
                case LogicOperator.ALL:
                    res = new FormulaNode(LogicOperator.EXISTS, this.childNodes[0].name);
                    break;
                case LogicOperator.AND:
                    res = new FormulaNode(LogicOperator.OR);
                    break;
                case LogicOperator.OR:
                    res = new FormulaNode(LogicOperator.AND);
                    break;

                case LogicOperator.NOT:
                    return this.childNodes[0].childNodes[0].NNF();

                case LogicOperator.IMP:
                    // ~(a->b) == a & ~b
                    res = new FormulaNode(LogicOperator.AND);
                    left = this.childNodes[0].childNodes[0].NNF();
                    right = new FormulaNode(LogicOperator.NOT);
                    right.SetChildren(this.childNodes[0].childNodes[1].NNF(), null);
                    res.SetChildren(left, right);
                    return res;

                case LogicOperator.EF:
                    res = new FormulaNode(LogicOperator.AG);
                    break;
                case LogicOperator.AF:
                    res = new FormulaNode(LogicOperator.EG);
                    break;
                case LogicOperator.EG:
                    res = new FormulaNode(LogicOperator.AF);
                    break;
                case LogicOperator.AG:
                    res = new FormulaNode(LogicOperator.EF);
                    break;
                case LogicOperator.AX:
                    res = new FormulaNode(LogicOperator.EX);
                    break;
                case LogicOperator.EX:
                    res = new FormulaNode(LogicOperator.AX);
                    break;
                case LogicOperator.EU:
                    res = new FormulaNode(LogicOperator.AR);
                    break;
                case LogicOperator.AU:
                    res = new FormulaNode(LogicOperator.ER);
                    break;
                case LogicOperator.ER:
                    res = new FormulaNode(LogicOperator.AU);
                    break;
                case LogicOperator.AR:
                    res = new FormulaNode(LogicOperator.EU);
                    break;

                case LogicOperator.VAR:
                    res = new FormulaNode(LogicOperator.NOT);
                    res.SetChildren(new FormulaNode(this.childNodes[0].name), null);
                    return res;

                default:
                    res = null;
                    break;
            }

            if (this.childNodes[0].childNodes[0] != null)
            {
                left = new FormulaNode(LogicOperator.NOT);
                left.SetChildren(this.childNodes[0].childNodes[0].NNF(), null);
                left = left.NNF();
            }
            if (this.childNodes[0].childNodes[1] != null)
            {
                right = new FormulaNode(LogicOperator.NOT);
                right.SetChildren(this.childNodes[0].childNodes[1].NNF(), null);
                right = right.NNF();
            }
            res.SetChildren(left, right);

            return res;
        }

        // Trnasfer the node to PNF (the formula is assumed to be in NNF)
        // We assume that the formula also has no temporal operators
        public FormulaNode PNF()
        {
            if (this.logicOp == LogicOperator.VAR)
                return new FormulaNode(this.name);

            FormulaNode res = null, left = null, right = null;
            ISet<string> leftSet, rightSet;
            string varname;

            if (this.logicOp == LogicOperator.NOT)
            {
                res = new FormulaNode(LogicOperator.NOT);
                res.SetChildren(new FormulaNode(this.childNodes[0].name), null);
                return res;
            }

            if (this.logicOp == LogicOperator.EXISTS || 
                this.logicOp == LogicOperator.ALL)
            {
                res = new FormulaNode(this.logicOp, this.name);
                res.SetChildren(this.childNodes[0].PNF(), null);
                return res;
            }

            // Since we assume NNF, all NOT nodes are next to variables
            // Thus all of the remaining operators are binary
            left = this.childNodes[0].PNF();
            right = this.childNodes[1].PNF();

            leftSet = left.GetVariables();
            rightSet = right.GetVariables();

            if (this.logicOp == LogicOperator.AND ||
                this.logicOp == LogicOperator.OR) 
            {
                FormulaNode temp;
                if (left.logicOp == LogicOperator.EXISTS ||
                    left.logicOp == LogicOperator.ALL) 
                {
                    if (rightSet.Contains(left.name))
                    {
                        varname = right.UniqueVariable();
                        left = left.Substitute(left.name, varname);
                    }

                    res = new FormulaNode(left.logicOp, left.name);
                    temp = new FormulaNode(this.logicOp);
                    temp.SetChildren(left.childNodes[0], right);
                    res.SetChildren(temp.PNF(), null);
                }
                else if (right.logicOp == LogicOperator.EXISTS ||
                         right.logicOp == LogicOperator.ALL)
                {
                    if (leftSet.Contains(right.name))
                    {
                        varname = left.UniqueVariable();
                        right = right.Substitute(right.name, varname);
                    }

                    res = new FormulaNode(right.logicOp, right.name);
                    temp = new FormulaNode(this.logicOp);
                    temp.SetChildren(left, right.childNodes[0]);
                    res.SetChildren(temp.PNF(), null);
                }
                else 
                {
                    res = new FormulaNode(this.logicOp);
                    res.SetChildren(left, right);
                }
            }

            // For convenience, we get rid of "->" operators
            if (this.logicOp == LogicOperator.IMP)
            {
                res = new FormulaNode(LogicOperator.OR);
                res.SetChildren(new FormulaNode(LogicOperator.NOT), right);
                res.childNodes[0].SetChildren(left, null);
                res = res.NNF().PNF();
            }

            return res;
        }

        // For a PNF formula, return the non-quanitifed part
        public FormulaNode GetPropositional()
        {
            FormulaNode res = this;

            while (res.logicOp == LogicOperator.EXISTS ||
                   res.logicOp == LogicOperator.ALL)
                res = res.childNodes[0];

            return res;
        }

    }

}
