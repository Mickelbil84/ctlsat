using System;
using System.IO;
using System.Collections.Generic;

namespace CTLSAT
{
    /*
     * A class for handling CNF conversion
     * 
     * We note that a CNF formula is represented
     * by a set of clauses, where each clause is also a set
     * 
     */
    public class FormulaCNF
    {
        private class TicketMachine
        {
            private int ticket;

            public TicketMachine()
            {
                ticket = 1;
            }

            public int GetTicket()
            {
                return ticket++;
            }

            public void Reset()
            {
                ticket = 1;
            }
        }

        private class TseytinBlock
        {
            public TseytinBlock(int ticket, string left, string right, LogicOperator logicOp)
            {
                this.ticket = ticket;
                this.left = left;
                this.right = right;
                this.logicOp = logicOp;
            }

            public override string ToString()
            {
                if (this.logicOp != LogicOperator.NOT)
                    return "x" + ticket + " <-> (" + left + " " + logicOp + " "
                        + right + ")";
                else
                    return "x" + ticket + " <-> ~" + left;
            }

            public int ticket;
            public string left, right;
            public LogicOperator logicOp;
        }

        // For a node subformula, assign a number n and return the block
        // xn <-> l op r
        private static TseytinBlock GetTseytinBlocks(FormulaNode node, 
                                                     ISet<TseytinBlock> blockSet)
        {
            if (node.GetLogicOperator() == LogicOperator.VAR)
                return null;

            int ticket = ticketMachine.GetTicket();
            string left = "", right = "";

            TseytinBlock temp;
            temp = GetTseytinBlocks(node.GetLeftChild(), blockSet);
            if (temp == null)
                left = node.GetLeftChild().GetName();
            else
                left = temp.ticket.ToString();

            if (node.GetRightChild() != null)
            {
                temp = GetTseytinBlocks(node.GetRightChild(), blockSet);
                if (temp == null)
                    right = node.GetRightChild().GetName();
                else
                    right = temp.ticket.ToString();
            }

            TseytinBlock res = new TseytinBlock(ticket, left, right, node.GetLogicOperator());
            blockSet.Add(res);

            return res;
        }

        private static ISet<ISet<string>> TseytinBlockToCNF(TseytinBlock block)
        {
            ISet<ISet<string>> res = new HashSet<ISet<string>>();
            ISet<string> temp;

            switch (block.logicOp)
            {
                case LogicOperator.NOT:
                    // t <-> ~l == (~t | ~l) & (t | l)
                    temp = new HashSet<string>();
                    temp.Add("-" + block.left);
                    temp.Add("-" + block.ticket);
                    res.Add(temp);
                    temp = new HashSet<string>();
                    temp.Add(block.left);
                    temp.Add(block.ticket.ToString());
                    res.Add(temp);
                    break; 

                case LogicOperator.OR:
                    // t <-> l|r == 
                    // (~t|l|r) & (~l|t) & (~r|t)
                    temp = new HashSet<string>();
                    temp.Add("-" + block.ticket);
                    temp.Add(block.left);
                    temp.Add(block.right);
                    res.Add(temp);
                    temp = new HashSet<string>();
                    temp.Add("-" + block.left);
                    temp.Add(block.ticket.ToString());
                    res.Add(temp);
                    temp = new HashSet<string>();
                    temp.Add("-" + block.right);
                    temp.Add(block.ticket.ToString());
                    res.Add(temp);
                    break;

                case LogicOperator.AND:
                    // t <-> l&r == 
                    // (t|~l|~r) & (l|~t) & (r|~t)
                    temp = new HashSet<string>();
                    temp.Add(block.ticket.ToString());
                    temp.Add("-" + block.left);
                    temp.Add("-" + block.right);
                    res.Add(temp);
                    temp = new HashSet<string>();
                    temp.Add(block.left);
                    temp.Add("-" + block.ticket);
                    res.Add(temp);
                    temp = new HashSet<string>();
                    temp.Add(block.right);
                    temp.Add("-" + block.ticket);
                    res.Add(temp);
                    break;

                default:
                    break;
            }

            return res;
        }

        private static TicketMachine ticketMachine = new TicketMachine();

        // Get a propositional formula and return its (raw) CNF form
        private static ISet<ISet<string>> TseytinTransformation(FormulaNode formula)
        {
            ISet<ISet<string>> res = new HashSet<ISet<string>>();
            ISet<TseytinBlock> blockSet = new HashSet<TseytinBlock>();
            ticketMachine.Reset();
            GetTseytinBlocks(formula, blockSet);

            foreach (TseytinBlock block in blockSet)
                res.UnionWith(TseytinBlockToCNF(block));

            return res;
        }

        public class QBCNFormula
        {
            public ISet<ISet<string>> propositional;
            public List<string> quantifiers;

            public override string ToString()
            {
                string res = "";
                int andcnt = 0;

                foreach (string q in quantifiers)
                    res += q + "\n";

                foreach (ISet<string> clause in this.propositional)
                {
                    res += "(";
                    int orcnt = 0;
                    foreach(string literal in clause)
                    {
                        res += literal;
                        orcnt++;
                        if (orcnt < clause.Count)
                            res += "|";
                    }
                    andcnt++;
                    res += ")";
                    if (andcnt < this.propositional.Count)
                        res += " &";
                    res += "\n";
                }

                return res;
            }

            public ISet<string> GetLiterals()
            {
                ISet<string> res = new HashSet<string>();

                foreach (ISet<string> clause in propositional)
                    res.UnionWith(clause);

                return res;
            }

            public void ReplaceLiterals(Dictionary<string, string> changes)
            {
                ISet<ISet<string>> newPropositional = new HashSet<ISet<string>>();
                List<string> newQuantifiers = new List<string>();

                foreach (ISet<string> clause in propositional)
                {
                    ISet<string> newClause = new HashSet<string>();
                    foreach(string literal in clause)
                    {
                        if (changes.ContainsKey(literal))
                            newClause.Add(changes[literal]);
                        else
                            newClause.Add(literal);
                    }
                    newPropositional.Add(newClause);
                }

                foreach (string q in this.quantifiers)
                {
                    string l = q.Substring(1);
                    // If the variable doesn't appear in the propositional
                    // formula, then there is no need to add it at all
                    if (changes.ContainsKey(l))
                        newQuantifiers.Add(q[0] + changes[l]);
                }

                this.propositional = newPropositional;
                this.quantifiers = newQuantifiers;
            }
        }

        // Get a PNF formula and return its QBCNF
        public static QBCNFormula ConvertToCNF(FormulaNode formula)
        {
            QBCNFormula res = new QBCNFormula();
            FormulaNode node = formula;

            res.quantifiers = new List<string>();
            while (node.GetLogicOperator() == LogicOperator.EXISTS ||
                   node.GetLogicOperator() == LogicOperator.ALL)
            {
                if (node.GetLogicOperator() == LogicOperator.EXISTS)
                    res.quantifiers.Add("e" + node.GetName());
                else 
                    res.quantifiers.Add("a" + node.GetName());
                node = node.GetLeftChild();
            }

            res.propositional = TseytinTransformation(node);

            //Replace strings with numbers
            ISet<string> literals = res.GetLiterals();
            Dictionary<string, string> changes = new Dictionary<string, string>();

            foreach (string literal in literals)
            {
                int n;
                bool isNum = int.TryParse(literal, out n);
                if (!changes.ContainsKey(literal) & !isNum)
                {
                    int ticket = ticketMachine.GetTicket();
                    if (literal[0] != '-')
                    {
                        changes.Add(literal, ticket.ToString());
                        changes.Add("-" + literal, (-ticket).ToString());
                    }
                    else 
                    {
                        changes.Add(literal, (-ticket).ToString());
                        changes.Add(literal.Substring(1), ticket.ToString());
                    }
                }
            }

            res.ReplaceLiterals(changes);

            return res;
        }

        public static void CreateQDIMACS(QBCNFormula qbcnf, string filename)
        {
            using (StreamWriter sw = File.CreateText(filename))
            {
                // Write preamble
                sw.WriteLine("p cnf " + qbcnf.quantifiers.Count +
                             " " + qbcnf.propositional.Count);

                // Write prefix
                char lastQuant = qbcnf.quantifiers[0][0];
                sw.Write(lastQuant + " " + qbcnf.quantifiers[0].Substring(1));
                for (int i = 1; i < qbcnf.quantifiers.Count; i++)
                {
                    if (qbcnf.quantifiers[i][0] != lastQuant)
                    {
                        sw.Write(" 0");
                        sw.WriteLine();
                        sw.Write(qbcnf.quantifiers[i][0] + " ");
                        lastQuant = qbcnf.quantifiers[i][0];
                    }

                    sw.Write(" " + qbcnf.quantifiers[i].Substring(1));
                }
                sw.Write(" 0");
                sw.WriteLine();

                // Write clauses
                foreach (ISet<string> clause in qbcnf.propositional) 
                {
                    foreach (string literal in clause)
                        sw.Write(literal + " ");
                    sw.Write("0");
                    sw.WriteLine();
                }
            }
        }
    }
}
