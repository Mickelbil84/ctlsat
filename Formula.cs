﻿using System;
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
        VAR
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
            this.logicOp = logicOp;
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

        public string ToString()
        {
            if (this.logicOp == LogicOperator.VAR)
                return this.name;

            if (this.childNodes[1] == null)
                return this.logicOp.ToString() + "(" + this.childNodes[0].ToString() + ")";

            string result = "";
            result += this.childNodes[0].ToString();
            result += " " + this.logicOp.ToString() + " ";
            result += this.childNodes[1].ToString();
            return result;
        }

    }

    public class Formula
    {
        private FormulaNode root = null;

        public Formula()
        {
        }

        public void CreateTestFormula()
        {
            root = new FormulaNode(LogicOperator.EX);
            root.SetChildren(null, new FormulaNode("b"));
            string res = root.ToString();
            Console.WriteLine(res);

            root = new FormulaNode(LogicOperator.EU);
            FormulaNode a = new FormulaNode(LogicOperator.AG);
            a.SetChildren(null, new FormulaNode("p"));

            root.SetChildren(a, new FormulaNode("q"));
            res = root.ToString();
            Console.WriteLine(res);
        }
    }
}
