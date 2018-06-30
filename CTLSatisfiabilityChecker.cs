using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTLSAT
{
    class CTLSatisfiabilityChecker
    {
        private TicketMachine uniqueId = new TicketMachine();
        private FormulaNode normalized;
        private ISet<FormulaNode> elementary;
        public CTLSatisfiabilityChecker(FormulaNode formula)
        {
            normalized = formula.NNF();
            elementary = CTLUtils.positiveElementary(normalized);
        }

        public void check(FormulaNode formula)
        {
            SymbolicState v = new SymbolicState(elementary, "v");

            // initially, all possible states belong to the structure
            FormulaNode states = new FormulaNode("TRUE");
            FormulaNode oldStates;

            while (true)
            {
                oldStates = states;
                FormulaNode succ = genSucc(states, v);
                states = new FormulaNode(LogicOperator.AND, states, succ);
            }
        }

        /* Generates a formula representing the fact that <state> has a
         * successor in <stateSet>. Does not guarantee that <state> itself
         * belongs to the set.
         */
        private FormulaNode genSucc(FormulaNode stateSet, SymbolicState state)
        {
            string nextName = "succ" + uniqueId.GetTicket().ToString();
            SymbolicState next = new SymbolicState(elementary, nextName);
            FormulaNode transition = genTransition(state, next);
            FormulaNode memberOf = SymbolicState.substitute(stateSet, state, next);
            FormulaNode result = new FormulaNode(LogicOperator.AND, transition, memberOf);
            result = next.quantify(LogicOperator.EXISTS, result);
            return result;
        }

        /* Generate the transition relation R */
        private FormulaNode genTransition(SymbolicState from, SymbolicState to)
        {
            List<FormulaNode> terms = new List<FormulaNode>();
            foreach (var e in elementary)
            {
                if (e.GetLogicOperator() != LogicOperator.EX)
                    continue;

                // if e = EX(g), generate the term "e(from) | ~g(to)"
                FormulaNode body = e[0];  // the formula inside the EX
                FormulaNode notBody = CTLUtils.nnfNegate(body);
                FormulaNode left = from.valueOf(e);
                FormulaNode right = to.valueOf(notBody);
                terms.Add(new FormulaNode(LogicOperator.OR, left, right));
            }

            return joinTerms(LogicOperator.AND, terms);
        }

        private FormulaNode joinTerms(LogicOperator op, List<FormulaNode> terms)
        {
            if (terms.Count == 0)
                throw new NotImplementedException();

            if (terms.Count == 1)
                return terms[0];

            FormulaNode result = new FormulaNode(op, terms[0], terms[1]);
            foreach (var t in terms.GetRange(2, terms.Count - 2))
            {
                result = new FormulaNode(op, result, t);
            }

            return result;
        }
    }
}
