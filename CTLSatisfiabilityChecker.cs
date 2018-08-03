﻿using System;
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
        private int iterations;

        private IDictionary<FormulaNode, FormulaNode> fragEU = new Dictionary<FormulaNode, FormulaNode>();
        private IDictionary<FormulaNode, FormulaNode> fragAU = new Dictionary<FormulaNode, FormulaNode>();

        public int Iterations { get { return iterations; }}

        public CTLSatisfiabilityChecker(FormulaNode formula)
        {
            normalized = formula.implementComplexOperators().NNF();
            elementary = CTLUtils.positiveElementary(normalized);
        }

        public bool check()
        {
            SymbolicState v = new SymbolicState(elementary, "v");

            // initially, all possible states belong to the structure
            FormulaNode states = new FormulaNode(FormulaNode.TRUE_LITERAL);
            FormulaNode oldStates;

            while (true)
            {
                iterations++;
#if DEBUG
                Console.WriteLine("Iteration " + iterations.ToString());
#endif
                oldStates = states;
                FormulaNode succ = genSucc(states, v);
                FormulaNode lc1 = genLC1(states, v);
                FormulaNode e = genE(states, v);
                FormulaNode a = genA(states, v);

                states = new FormulaNode(LogicOperator.AND, states, succ);
                states = new FormulaNode(LogicOperator.AND, states, lc1);
                states = new FormulaNode(LogicOperator.AND, states, e);
                states = new FormulaNode(LogicOperator.AND, states, a);
                if (isFixpoint(oldStates, states, v))
                    break;
            }
#if DEBUG
            Console.WriteLine("Reached fixpoint. Checking for satisfying state");
#endif
            FormulaNode formulaValue = v.valueOf(normalized);
            FormulaNode formulaAndValid = new FormulaNode(LogicOperator.AND, states, formulaValue);
            FormulaNode sat = v.quantify(LogicOperator.EXISTS, formulaAndValid);
            return FormulaCNF.QBFSAT(sat);
        }

        /* Check if we reached a fixpoint. That is, if there are no states in <largeSet>
         * which are absent from <smallSet>.
         */
        private bool isFixpoint(FormulaNode largeSet, FormulaNode smallSet, SymbolicState v)
        {
            FormulaNode notNew = new FormulaNode(LogicOperator.NOT, smallSet, null);
            FormulaNode stateRemoved = new FormulaNode(LogicOperator.AND, largeSet, notNew);
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

        private void ComputeEUFragments(FormulaNode stateSet, SymbolicState state)
        {
            Console.WriteLine("Compute fragments");
            foreach (var e in elementary)
            {
                if (e.GetLogicOperator() != LogicOperator.EX)
                    continue;
                if (e[0].GetLogicOperator() != LogicOperator.EU)
                    continue;
#if DEBUG
                Console.WriteLine("Computing fragment for " + e);
#endif
                FormulaNode frag = FormulaParser.parse("~TRUE");
                while (true)
                {
                    string nextName = "succ" + uniqueId.GetTicket().ToString();
                    SymbolicState next = new SymbolicState(elementary, nextName);
                    FormulaNode transition = genTransition(state, next);
                    FormulaNode memberOf = SymbolicState.substitute(stateSet, state, next);
                    FormulaNode nextFrag = SymbolicState.substitute(frag, state, next);
                    FormulaNode newFrag = new FormulaNode(LogicOperator.AND, transition, memberOf);
                    newFrag = new FormulaNode(LogicOperator.AND, newFrag, nextFrag);
                    newFrag = next.quantify(LogicOperator.EXISTS, newFrag);

                    newFrag = new FormulaNode(LogicOperator.AND, state.valueOf(e[0][0]), newFrag);
                    newFrag = new FormulaNode(LogicOperator.OR, state.valueOf(e[0][1]), newFrag);

                    if (isFixpoint(newFrag, frag, state))
                        break;

                    frag = newFrag;
                }
                fragEU[e[0]] = frag;
            }
        }

        /* Generates a formula representing the fact that for <state> either
         * the eventuality is not promised or that if it is, then there 
         * exists a fragment of <stateSet> rooted at <state> that has
         * a frontier node who satisfied the eventuallity
         */
        private FormulaNode genE(FormulaNode stateSet, SymbolicState state)
        {
            ComputeEUFragments(stateSet, state);

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

                FormulaNode frag = fragEU[e[0]];

                res = new FormulaNode(LogicOperator.OR, res, frag);
                terms.Add(res);

            }
            return joinTerms(LogicOperator.AND, terms);
        }

        /*
         * <au> is an elementary formula of the form EX(ER(p,q))
         */
        private FormulaNode ComputeAUFragment(FormulaNode au, FormulaNode stateSet, SymbolicState state)
        {
#if DEBUG
            Console.WriteLine("Computing fragment for " + au);
#endif
            FormulaNode frag = FormulaParser.parse("~TRUE");
            FormulaNode notLeft = new FormulaNode(LogicOperator.NOT, au[0][0], null);
            FormulaNode notRight = new FormulaNode(LogicOperator.NOT, au[0][1], null);
            while (true)
            {
                string nextName = "succ" + uniqueId.GetTicket().ToString();
                SymbolicState next = new SymbolicState(elementary, nextName);
                FormulaNode transition = genTransition(state, next);
                FormulaNode memberOf = SymbolicState.substitute(stateSet, state, next);
                FormulaNode nextFrag = SymbolicState.substitute(frag, state, next);
                FormulaNode newFrag = new FormulaNode(LogicOperator.AND, transition, memberOf);
                newFrag = new FormulaNode(LogicOperator.AND, newFrag, nextFrag);
                newFrag = next.quantify(LogicOperator.EXISTS, newFrag);

                newFrag = new FormulaNode(LogicOperator.AND, state.valueOf(notLeft.NNF()), newFrag);

                // Add the big conjunction needed for fragAU
                List<FormulaNode> fragTerms = new List<FormulaNode>();

                foreach (FormulaNode el in elementary)
                {
                    if (el.GetLogicOperator() != LogicOperator.EX)
                        continue;

                    nextName = "succ" + uniqueId.GetTicket().ToString();
                    next = new SymbolicState(elementary, nextName);
                    transition = genTransition(state, next);
                    memberOf = SymbolicState.substitute(stateSet, state, next);
                    nextFrag = SymbolicState.substitute(frag, state, next);


                    FormulaNode lhs = state.valueOf(el);
                    FormulaNode rhs = new FormulaNode(LogicOperator.AND, transition, memberOf);
                    rhs = new FormulaNode(LogicOperator.AND, rhs, nextFrag);
                    rhs = new FormulaNode(LogicOperator.AND, rhs, next.valueOf(SymbolicState.substitute(el[0], state, next)));
                    rhs = next.quantify(LogicOperator.EXISTS, rhs);

                    fragTerms.Add(new FormulaNode(LogicOperator.IMP, lhs, rhs));
                }

                newFrag = new FormulaNode(LogicOperator.AND, newFrag, joinTerms(LogicOperator.AND, fragTerms));
                newFrag = new FormulaNode(LogicOperator.OR, state.valueOf(notRight.NNF()), newFrag);

                if (isFixpoint(newFrag, frag, state))
                    return frag;

                frag = newFrag;
            }

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

                FormulaNode frag = ComputeAUFragment(e, stateSet, state);

                FormulaNode promised = state.valueOf(e);

                FormulaNode res = new FormulaNode(LogicOperator.OR, promised, state.valueOf(e[0][0]));

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

        // TODO: Move to Formula.cs
        public static FormulaNode joinTerms(LogicOperator op, List<FormulaNode> terms)
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
