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

        private static Dictionary<string, BinaryToken> binaryOps = new Dictionary<string, BinaryToken>
        {
            ["&"] = new BinaryToken("&", 3, LogicOperator.AND),
            ["|"] = new BinaryToken("|", 2, LogicOperator.OR),
            [","] = new BinaryToken(",", 1, LogicOperator.COMMA)
        };

        private static Dictionary<string, UnaryToken> unaryOps = new Dictionary<string, UnaryToken>
        {
            ["~"] = new UnaryToken(LogicOperator.NOT),
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
            LogicOperator.AU, LogicOperator.EU
        };

        private static List<Token> toplevelTokenize(string str)
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
                    tokens.Add(identifyToken(token));
                    token = "";
                    continue;
                }

                if (ch == ')' && nest == 0)
                {
                    tokens.Add(new LiteralToken(TokenType.UNPARSED, token));
                    token = "";
                    continue;
                }

                if (Char.IsLetterOrDigit(ch) || nest > 0)
                    token += ch;
                else
                {
                    if (token != "")
                        tokens.Add(identifyToken(token));
                    tokens.Add(identifyToken(ch.ToString()));
                    token = "";
                }

            }
            if (token != "")
                tokens.Add(identifyToken(token));
            return tokens;
        }

        private static Token identifyToken(string token)
        {
            if (binaryOps.ContainsKey(token))
                return binaryOps[token];

            if (unaryOps.ContainsKey(token))
                return unaryOps[token];

            return new LiteralToken(TokenType.ATOM, token);
        }

        public static FormulaNode parse(string str)
        {
            return parse(toplevelTokenize(str));
        }

        private static FormulaNode parse(List<Token> tokens)
        {
            FormulaNode result = null;
            List<Token> rightSide = new List<Token>();
            BinaryToken lastOp = null;

            // a single token - must be an atom or an unparsed string
            if (tokens.Count == 1)
            {
                var tok = tokens[0] as LiteralToken;
                if (tok.type == TokenType.ATOM)
                    return new FormulaNode(tok.value);

                return parse(toplevelTokenize(tok.value));
            }

            // unary operator - the rest is the operand
            if (tokens[0] is UnaryToken)
            {
                var opToken = tokens[0] as UnaryToken;
                result = new FormulaNode(opToken.logicOperator);
                var operand = parse(tokens.GetRange(1, tokens.Count - 1));
                if (untilOperators.Contains(opToken.logicOperator))
                    result.SetChildren(operand[0], operand[1]);
                else
                    result.SetChildren(operand, null);
                return result;
            }

            // more than two tokens - split into parts by binary operators
            int minPrecedence = 1000;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] is BinaryToken)
                {
                    var op = tokens[i] as BinaryToken;
                    minPrecedence = Math.Min(minPrecedence, op.precedence);
                }
            }

            foreach (Token t in tokens)
            {
                if (t is BinaryToken)
                {
                    var op = t as BinaryToken;
                    if (op.precedence == minPrecedence)
                    {
                        if (result == null)
                        {
                            result = parse(rightSide);
                        }
                        else
                        {
                            var leftSide = result;
                            result = new FormulaNode(lastOp.logicOperator);
                            result.SetChildren(leftSide, parse(rightSide));
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
                result.SetChildren(leftSide, parse(rightSide));
            }
            return result;
        }

    }
}