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

        private IDictionary<FormulaNode, FormulaNode> fragEU = new Dictionary<FormulaNode, FormulaNode>();
        private IDictionary<FormulaNode, FormulaNode> fragAU = new Dictionary<FormulaNode, FormulaNode>();

        public CTLSatisfiabilityChecker(FormulaNode formula)
        {
            normalized = formula.NNF();
            elementary = CTLUtils.positiveElementary(normalized);
            Console.WriteLine("Positive Elementary:");
            foreach (var v in elementary)
                Console.WriteLine(" " + v.ToString());
        }

        public bool check()
        {
            SymbolicState v = new SymbolicState(elementary, "v");

            // initially, all possible states belong to the structure
            FormulaNode states = new FormulaNode(FormulaNode.TRUE_LITERAL);
            FormulaNode oldStates;

            int i = 0;
            while (true)
            {
                i++;
                Console.WriteLine("Iteration " + i.ToString());
                oldStates = states;
                FormulaNode succ = genSucc(states, v);
                FormulaNode lc1 = genLC1(states, v);
                FormulaNode e = genE(states, v);
                FormulaNode a = genA(states, v);
                states = new FormulaNode(LogicOperator.AND, states, succ);
                states = new FormulaNode(LogicOperator.AND, states, lc1);
                states = new FormulaNode(LogicOperator.AND, states, e);
                states = new FormulaNode(LogicOperator.AND, states, a);
                if (isFixpoint(states, oldStates, v))
                    break;
            }

            Console.WriteLine("Reached fixpoint. Checking for satisfying state");
            FormulaNode formulaValue = v.valueOf(normalized);
            FormulaNode formulaAndValid = new FormulaNode(LogicOperator.AND, states, formulaValue);
            FormulaNode sat = v.quantify(LogicOperator.EXISTS, formulaAndValid);
            return FormulaCNF.QBFSAT(sat);
        }

        /* Check if we reached a fixpoint. That is, if no states were removed from
         * <oldStates>.
         */
        private bool isFixpoint(FormulaNode oldStates, FormulaNode newStates, SymbolicState v)
        {
            FormulaNode notNew = new FormulaNode(LogicOperator.NOT, newStates, null);
            FormulaNode stateRemoved = new FormulaNode(LogicOperator.AND, oldStates, notNew);
            stateRemoved = v.quantify(LogicOperator.EXISTS, stateRemoved);
            return !FormulaCNF.QBFSAT(stateRemoved);
        }

        private FormulaNode genLC1(FormulaNode stateSet, SymbolicState state)
        {
            List<FormulaNode> terms = new List<FormulaNode>();
            foreach (var e in elementary)
            {
                if (e.GetLogicOperator() != LogicOperator.EX)
                    continue;

                string nextName = "succ" + uniqueId.GetTicket().ToString();
                SymbolicState next = new SymbolicState(elementary, nextName);
                FormulaNode existsInState = state.valueOf(e);
                FormulaNode notExistsInState = new FormulaNode(LogicOperator.NOT, existsInState, null);
                FormulaNode nextValid = SymbolicState.substitute(stateSet, state, next);
                FormulaNode transition = genTransition(state, next);
                FormulaNode body = e[0];
                FormulaNode occursNext = next.valueOf(body);
                FormulaNode hasSucc = new FormulaNode(LogicOperator.AND, nextValid, transition);
                hasSucc = new FormulaNode(LogicOperator.AND, hasSucc, occursNext);
                hasSucc = next.quantify(LogicOperator.EXISTS, hasSucc);
                terms.Add(new FormulaNode(LogicOperator.OR, notExistsInState, hasSucc));
            }
            return joinTerms(LogicOperator.AND, terms);
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

        /* Generates a formula representing the fact that for <state> either
         * the eventuality is not promised or that if it is, then there 
         * exists a fragment of <stateSet> rooted at <state> that has
         * a frontier node who satisfied the eventuallity
         */
        private FormulaNode genE(FormulaNode stateSet, SymbolicState state)
        {
            List<FormulaNode> terms = new List<FormulaNode>();
            foreach (var e in elementary)
            {
                if (e.GetLogicOperator() != LogicOperator.EX)
                    continue;
                if (e[0].GetLogicOperator() != LogicOperator.EU)
                    continue;

                FormulaNode notPromised = new FormulaNode(LogicOperator.NOT);
                notPromised.SetChildren(state.valueOf(e), null);

                FormulaNode res = new FormulaNode(LogicOperator.OR);
                res.SetChildren(notPromised, new FormulaNode(LogicOperator.NOT));
                res[1].SetChildren(state.valueOf(e[0][0]), null);

                // Create the fragEU iteration
                if (!fragEU.Keys.Contains(e[0]))
                    fragEU[e[0]] = new FormulaNode(FormulaNode.TRUE_LITERAL);
                FormulaNode frag = fragEU[e[0]];
                string nextName = "succ" + uniqueId.GetTicket().ToString();
                SymbolicState next = new SymbolicState(elementary, nextName);
                FormulaNode transition = genTransition(state, next);
                FormulaNode memberOf = SymbolicState.substitute(stateSet, state, next);
                FormulaNode newFrag = SymbolicState.substitute(frag, state, next);
                frag = new FormulaNode(LogicOperator.AND, transition, memberOf);
                frag = new FormulaNode(LogicOperator.AND, frag, newFrag);
                frag = next.quantify(LogicOperator.EXISTS, frag);

                frag = new FormulaNode(LogicOperator.AND, state.valueOf(e[0][0]), frag);
                frag = new FormulaNode(LogicOperator.OR, state.valueOf(e[0][1]), frag);

                fragEU[e[0]] = frag;

                res = new FormulaNode(LogicOperator.OR, res, frag);
                terms.Add(res);

            }
            return joinTerms(LogicOperator.AND, terms);
        }

        /* Generates a formula representing the fact that for <state> either
         * the eventuality is not promised or that if it is, then there 
         * exists a fragment of <stateSet> rooted at <state> such that
         * all the frontier nodes satisfy the eventuallity
         */
        private FormulaNode genA(FormulaNode stateSet, SymbolicState state)
        {
            List<FormulaNode> terms = new List<FormulaNode>();
            foreach (var e in elementary)
            {
                if (e.GetLogicOperator() != LogicOperator.EX)
                    continue;
                if (e[0].GetLogicOperator() != LogicOperator.ER)
                    continue;

                FormulaNode notPromised = state.valueOf(e);

                FormulaNode res = new FormulaNode(LogicOperator.OR);
                res.SetChildren(notPromised, state.valueOf(e[0][0]));

                // Create the fragAU iteration
                FormulaNode fragKey = new FormulaNode(LogicOperator.AU);
                fragKey.SetChildren(new FormulaNode(LogicOperator.NOT), new FormulaNode(LogicOperator.NOT));
                fragKey[0].SetChildren(e[0][0], null);
                fragKey[1].SetChildren(e[0][1], null);

                if (!fragAU.Keys.Contains(fragKey))
                    fragAU[fragKey] = new FormulaNode(FormulaNode.TRUE_LITERAL);
                FormulaNode frag = fragAU[fragKey];
                string nextName = "succ" + uniqueId.GetTicket().ToString();
                SymbolicState next = new SymbolicState(elementary, nextName);
                FormulaNode transition = genTransition(state, next);
                FormulaNode memberOf = SymbolicState.substitute(stateSet, state, next);
                FormulaNode newFrag = SymbolicState.substitute(frag, state, next);
                frag = new FormulaNode(LogicOperator.AND, transition, memberOf);
                frag = new FormulaNode(LogicOperator.AND, frag, newFrag);
                frag = next.quantify(LogicOperator.EXISTS, frag);

                frag = new FormulaNode(LogicOperator.AND, state.valueOf(fragKey[0].NNF()), frag);
                frag = new FormulaNode(LogicOperator.OR, state.valueOf(fragKey[1].NNF()), frag);

                // Add the big conjunction needed for fragAU
                List<FormulaNode> fragTerms = new List<FormulaNode>();

                foreach (FormulaNode el in elementary)
                {
                    if (el.GetLogicOperator() != LogicOperator.EX)
                        continue;

                    FormulaNode lhs = state.valueOf(el);
                    FormulaNode rhs = new FormulaNode(LogicOperator.AND, transition, memberOf);
                    rhs = new FormulaNode(LogicOperator.AND, rhs, newFrag);
                    rhs = new FormulaNode(LogicOperator.AND, rhs, next.valueOf(SymbolicState.substitute(el[0], state, next)));
                    rhs = next.quantify(LogicOperator.EXISTS, rhs);

                    fragTerms.Add(new FormulaNode(LogicOperator.IMP, lhs, rhs));
                }

                frag = new FormulaNode(LogicOperator.AND, frag, joinTerms(LogicOperator.AND, fragTerms));

                fragAU[fragKey] = frag;

                res = new FormulaNode(LogicOperator.OR, res, frag);
                terms.Add(res);

            }
            return joinTerms(LogicOperator.AND, terms);
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
            FormulaNode result;
            if (terms.Count == 0)
            {
                if (op == LogicOperator.AND)
                    return new FormulaNode(FormulaNode.TRUE_LITERAL);
                if (op == LogicOperator.OR)
                {
                    // Build FALSE as ~TRUE
                    result = new FormulaNode(LogicOperator.NOT);
                    result.SetChildren(new FormulaNode(FormulaNode.TRUE_LITERAL), null);
                    return result;
                }

                throw new Exception("Join with unsupported logic operator");
            }
                

            if (terms.Count == 1)
                return terms[0];

            result = new FormulaNode(op, terms[0], terms[1]);
            foreach (var t in terms.GetRange(2, terms.Count - 2))
            {
                result = new FormulaNode(op, result, t);
            }

            return result;
        }
    }
}
