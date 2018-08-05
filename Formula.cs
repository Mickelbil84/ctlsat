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
        public const string TRUE_LITERAL = "TRUE";

        private FormulaNode[] childNodes = new FormulaNode[2];

        private LogicOperator logicOp;
        private ISet<string> variableSet;
        private static TicketMachine nameGenerator = new TicketMachine();

        private string name;
        public FormulaNode() {}
        public FormulaNode(string name)
        {
            this.name = name;
            this.logicOp = LogicOperator.VAR;
            this.variableSet = null;
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

        public FormulaNode(LogicOperator logicOp, FormulaNode left, FormulaNode right)
        {
            this.logicOp = logicOp;
            childNodes[0] = left;
            childNodes[1] = right;
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

        public LogicOperator GetLogicOperator()
        {
            return this.logicOp;
        }

        public string GetName()
        {
            return this.name;
        }

        public void SetName(string name)
        {
            this.name = name;
            this.variableSet = null;
        }

        public void SetVariable(string name)
        {
            this.name = name;
            this.logicOp = LogicOperator.VAR;
            this.variableSet = null;
        }

        public void SetChildren(FormulaNode left, FormulaNode right)
        {
            this.childNodes[0] = left;
            this.childNodes[1] = right;
            this.variableSet = null;
        }

        public override bool Equals(Object other)
        {
            var otherFormula = other as FormulaNode;
            if (otherFormula.logicOp != logicOp)
                return false;

            if (logicOp == LogicOperator.VAR)
                return name == otherFormula.name;

            if (!childNodes[0].Equals(otherFormula.childNodes[0]))
                return false;

            if (childNodes[1] == null && otherFormula[1] == null)
                return true;

            return childNodes[1].Equals(otherFormula[1]);
        }

        public override int GetHashCode()
        {
            return ((int)logicOp);
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
            if (this.childNodes[0].logicOp != this.logicOp && this.childNodes[0].logicOp != LogicOperator.VAR)
                result += "(" + this.childNodes[0].ToString() + ")";
            else
                result += this.childNodes[0].ToString();
            result += " " + opstring + " ";
            if (this.childNodes[1].logicOp != this.logicOp && this.childNodes[1].logicOp != LogicOperator.VAR)
                result += "(" + this.childNodes[1].ToString() + ")";
            else
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

            if (this.variableSet != null)
                return this.variableSet;

            if (this.logicOp == LogicOperator.VAR)
            {
                if (this.name != TRUE_LITERAL)
                    res.Add(this.name);
                return res;
            }

            if (this.logicOp == LogicOperator.EXISTS ||
                this.logicOp == LogicOperator.ALL)
                res.Add(this.name);

            for (int i = 0; i < 2; i++)
                if (this.childNodes[i] != null)
                    res.UnionWith(this.childNodes[i].GetVariables());

            this.variableSet = res;

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

        // Return a new temporary variable that wasn't used in any PNF conversion before
        private static string UniquePNFVariable()
        {
            return "P" + nameGenerator.GetTicket().ToString();
        }

        // replace ->, AF, AG, EF and EG with simpler operators
        public FormulaNode implementComplexOperators()
        {
            FormulaNode leftChild = null;
            FormulaNode rightChild = null;
            FormulaNode trueFormula = new FormulaNode(TRUE_LITERAL);
            FormulaNode falseFormula = new FormulaNode(LogicOperator.NOT, trueFormula, null);
            FormulaNode result;
            if (this[0] != null)
                leftChild = this[0].implementComplexOperators();
            if (this[1] != null)
                rightChild = this[1].implementComplexOperators();
            switch (logicOp) {
                // (a -> b) is (~a | b)
                case LogicOperator.IMP:
                    FormulaNode left = new FormulaNode(LogicOperator.NOT, leftChild, null);
                    return new FormulaNode(LogicOperator.OR, left, rightChild);

                // EF(f) is EU(TRUE, f)
                case LogicOperator.EF:
                    return new FormulaNode(LogicOperator.EU, trueFormula, leftChild);

                // EG(f) is ER(FALSE, f)
                case LogicOperator.EG:
                    return new FormulaNode(LogicOperator.ER, falseFormula, leftChild);

                // AF(f) is AU(TRUE, f)
                case LogicOperator.AF:
                    return new FormulaNode(LogicOperator.AU, trueFormula, leftChild);

                // AG(f) is AU(FALSE, f)
                case LogicOperator.AG:
                    return new FormulaNode(LogicOperator.AR, falseFormula, leftChild);

                default:
                    result = new FormulaNode(logicOp, name);
                    result.SetChildren(leftChild, rightChild);
                    return result;
            }
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
                    res.SetChildren(left, right.NNF());
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
                        varname = UniquePNFVariable();
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
                        varname = UniquePNFVariable();
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

        /*public FormulaNode PNF()
        {
            return this.FastPNF();
        }

        private static ISet<string> variables;

        private FormulaNode FastPNFRec()
        {
            if (this.logicOp == LogicOperator.VAR)
                return new FormulaNode(this.name);

            FormulaNode res = null, left = null, right = null;
            //string varname;

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
                res.SetChildren(this.childNodes[0].FastPNFRec(), null);
                return res;
            }

            // Since we assume NNF, all NOT nodes are next to variables
            // Thus all of the remaining operators are binary
            left = this.childNodes[0].FastPNFRec();
            right = this.childNodes[1].FastPNFRec();

#if DEBUG
            if (!VerifyDisjointQuantifiers(left, right))
                throw new Exception("PNF error");
#endif

            if (this.logicOp == LogicOperator.AND ||
                this.logicOp == LogicOperator.OR)
            {
                FormulaNode temp, ptr = null;
                while (left.logicOp == LogicOperator.EXISTS ||
                    left.logicOp == LogicOperator.ALL)
                {
                    if (variables.Contains(left.name))
                    {
                        //varname = right.UniqueVariable();
                        //left = left.Substitute(left.name, varname);
                        //variables.Add(varname);
                    }

                    temp = new FormulaNode(left.logicOp, left.name);
                    if (res == null)
                        res = ptr = temp;
                    else 
                    {
                        ptr.childNodes[0] = temp;
                        ptr = ptr[0];
                    }

                    left = left[0];
                }

                while (right.logicOp == LogicOperator.EXISTS ||
                    right.logicOp == LogicOperator.ALL)
                {
                    if (variables.Contains(right.name))
                    {
                        //varname = left.UniqueVariable();
                        //right = right.Substitute(right.name, varname);
                        //variables.Add(varname);
                    }

                    temp = new FormulaNode(right.logicOp, right.name);
                    if (res == null)
                        res = ptr = temp;
                    else
                    {
                        ptr.childNodes[0] = temp;
                        ptr = ptr[0];
                    }

                    right = right[0];
                }

                if (res == null)
                {
                    res = new FormulaNode(this.logicOp);
                    res.SetChildren(left, right);
                }
                else
                {
                    ptr.childNodes[0] = new FormulaNode(this.logicOp);
                    ptr.childNodes[0].SetChildren(left, right);
                }
            }

            // For convenience, we get rid of "->" operators
            if (this.logicOp == LogicOperator.IMP)
            {
                res = new FormulaNode(LogicOperator.OR);
                res.SetChildren(new FormulaNode(LogicOperator.NOT), right);
                res.childNodes[0].SetChildren(left, null);
                res = res.NNF().FastPNFRec();
            }

            return res;
        }

        // Tests whether variables that are quantified in <left> don't appear in <right>
        // (quantified or not) and vice versa.
        // Assumes <left> and <right> are in PNF form.
        private bool VerifyDisjointQuantifiers(FormulaNode left, FormulaNode right)
        {
            ISet<string> leftVars = left.GetVariables();
            ISet<string> rightVars = right.GetVariables();
            ISet<string> leftQuantified = left.GetLeadingQuantifiedVars();
            ISet<string> rightQuantified = right.GetLeadingQuantifiedVars();

            leftVars.IntersectWith(rightQuantified);
            if (leftVars.Count > 0)
                return false;

            rightVars.IntersectWith(leftQuantified);
            if (rightVars.Count > 0)
                return false;
            return true;
        }

        private ISet<string> GetLeadingQuantifiedVars()
        {
            ISet<string> result = new HashSet<string>();
            FormulaNode ptr = this;
            while (ptr.logicOp == LogicOperator.EXISTS ||
                    ptr.logicOp == LogicOperator.ALL)
            {
                result.Add(ptr.name);
                ptr = ptr[0];
            }
            return result;
        }

        public FormulaNode FastPNF()
        {
            variables = this.GetVariables();
            return this.FastPNFRec();
        }*/


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
