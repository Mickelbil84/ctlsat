using System;
using System.Collections.Generic;


namespace CTLSAT
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            CTLTests.TestArticle();
            CTLTests.ShowLog();
        }

        public static string convSAT(bool b)
        {
            if (b) return "SAT";
            return "UNSAT";
        }
    }
}
