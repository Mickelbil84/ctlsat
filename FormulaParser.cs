using System;
using System.Collections.Generic;
using System.Linq;

namespace CTLSAT
{
    public class FormulaParser
    {
        private enum TokenType
        {
            UNPARSED, ATOM
        }

        private class Token { }

        private class LiteralToken : Token
        {
            public TokenType type;
            public string value;

            public LiteralToken(TokenType type, string value)
            {
                this.type = type;
                this.value = value;
            }

        }

        private class BinaryToken : Token
        {
            public readonly string name;
            public readonly int precedence;
            public readonly LogicOperator logicOperator;

            public BinaryToken(string name, int precedence, LogicOperator logicOperator)
            {
                this.name = name;
                this.precedence = precedence;
                this.logicOperator = logicOperator;
            }
        }

        private class UnaryToken : Token
        {
            public LogicOperator logicOperator;

            public UnaryToken(LogicOperator logicOperator)
            {
                this.logicOperator = logicOperator;
            }
        }

        private const int MAX_PRECEDENCE = 1000;
        private static Dictionary<string, BinaryToken> binaryOps = new Dictionary<string, BinaryToken>
        {
            ["&"] = new BinaryToken("&", 3, LogicOperator.AND),
            ["|"] = new BinaryToken("|", 2, LogicOperator.OR),
            [","] = new BinaryToken(",", 1, LogicOperator.COMMA)
        };

        private static Dictionary<string, UnaryToken> unaryOps = new Dictionary<string, UnaryToken>
        {
            ["~"] = new UnaryToken(LogicOperator.NOT),
            ["E"] = new UnaryToken(LogicOperator.EXISTS),
            ["A"] = new UnaryToken(LogicOperator.ALL),
            ["AG"] = new UnaryToken(LogicOperator.AG),
            ["AU"] = new UnaryToken(LogicOperator.AU),
            ["AX"] = new UnaryToken(LogicOperator.AX),
            ["AF"] = new UnaryToken(LogicOperator.AF),
            ["AR"] = new UnaryToken(LogicOperator.AR),
            ["EG"] = new UnaryToken(LogicOperator.EG),
            ["EU"] = new UnaryToken(LogicOperator.EU),
            ["EX"] = new UnaryToken(LogicOperator.EX),
            ["EF"] = new UnaryToken(LogicOperator.EF),
            ["ER"] = new UnaryToken(LogicOperator.ER)
        };

        private static List<LogicOperator> untilOperators = new List<LogicOperator>
        {
            LogicOperator.AU, LogicOperator.EU, LogicOperator.AR, LogicOperator.ER
        };

        private static List<LogicOperator> quanitifers = new List<LogicOperator>
        {
            LogicOperator.EXISTS, LogicOperator.ALL
        };

        /* Split the given string to tokens, regarding parenthesised substring
         * as a single token.
         */
        private static List<Token> ToplevelTokenize(string str)
        {
            List<Token> tokens = new List<Token>();
            string token = "";
            int nest = 0;
            foreach (char ch in str)
            {
                if (ch == ' ')
                    continue;

                if (ch == '(')
                    nest++;
                if (ch == ')')
                    nest--;

                if (ch == '(' && nest == 1)
                {
                    if (token != "")
                        tokens.Add(IdentifyToken(token));
                    token = "";
                    continue;
                }

                if (ch == ')' && nest == 0)
                {
                    tokens.Add(new LiteralToken(TokenType.UNPARSED, token));
                    token = "";
                    continue;
                }

                if (Char.IsLetterOrDigit(ch) || ch == '_' || nest > 0)
                    token += ch;
                else
                {
                    if (token != "")
                        tokens.Add(IdentifyToken(token));
                    tokens.Add(IdentifyToken(ch.ToString()));
                    token = "";
                }

            }
            if (token != "")
                tokens.Add(IdentifyToken(token));
            return tokens;
        }

        private static Token IdentifyToken(string token)
        {
            if (binaryOps.ContainsKey(token))
                return binaryOps[token];

            if (unaryOps.ContainsKey(token))
                return unaryOps[token];

            return new LiteralToken(TokenType.ATOM, token);
        }

        public static FormulaNode Parse(string str)
        {
            return Parse(ToplevelTokenize(str));
        }

        private static FormulaNode Parse(List<Token> tokens)
        {
            FormulaNode result = null;
            List<Token> rightSide = new List<Token>();
            BinaryToken lastOp = null;

            // the input is a single token - must be an atom or an unparsed string
            if (tokens.Count == 1)
            {
                var tok = tokens[0] as LiteralToken;
                if (tok.type == TokenType.ATOM)
                    return new FormulaNode(tok.value);

                return Parse(ToplevelTokenize(tok.value));
            }

            int minPrecedence = MAX_PRECEDENCE;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] is BinaryToken)
                {
                    var op = tokens[i] as BinaryToken;
                    minPrecedence = Math.Min(minPrecedence, op.precedence);
                }
            }

            if (minPrecedence == MAX_PRECEDENCE)
            {
                // the input didn't contain any toplevel binary operators
                var opToken = tokens[0] as UnaryToken;
                result = new FormulaNode(opToken.logicOperator);
                var operand = Parse(tokens.GetRange(1, tokens.Count - 1));
                if (untilOperators.Contains(opToken.logicOperator))
                    result.SetChildren(operand[0], operand[1]);
                else if (quanitifers.Contains(opToken.logicOperator))
                {
                    result.SetName(operand[0].GetName());
                    result.SetChildren(operand[1], null);
                }
                else
                    result.SetChildren(operand, null);
                return result;
            }

            // if we got here - split by lowest-precedence binary operator
            foreach (Token t in tokens)
            {
                if (t is BinaryToken)
                {
                    var op = t as BinaryToken;
                    if (op.precedence == minPrecedence)
                    {
                        if (result == null)
                        {
                            result = Parse(rightSide);
                        }
                        else
                        {
                            var leftSide = result;
                            result = new FormulaNode(lastOp.logicOperator);
                            result.SetChildren(leftSide, Parse(rightSide));
                        }
                        rightSide = new List<Token>();
                        lastOp = op;
                        continue;
                    }
                }
                rightSide.Add(t);
            }
            if (rightSide.Count != 0)
            {
                var leftSide = result;
                result = new FormulaNode(lastOp.logicOperator);
                result.SetChildren(leftSide, Parse(rightSide));
            }

            return result;
        }

    }
}