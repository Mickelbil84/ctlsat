using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;

namespace CTLSAT
{
    /*
     * A class that provides a simple interface for the external 
     * QBF Solver.
     * 
     * Assumption: We assume that right next to the executable,
     * there exists an executable file named "solver".
     */
    public class QBFSolver
    {
        private const string solverFile = "solver";
        private const string tempFile = "temp.qdimacs";
        private const int retSAT = 10; //As defined by qbfeval
        private const int retUNSAT = 20;

        private static Process solverThread;

        private static void CreateQDIMACS(FormulaCNF.QBCNFormula qbcnf, string filename)
        {
            using (StreamWriter sw = File.CreateText(filename))
            {
                // Write preamble
                sw.WriteLine("p cnf " + qbcnf.quantifiers.Count +
                             " " + qbcnf.propositional.Count);

                // Write prefix
                if (qbcnf.quantifiers.Count > 0)
                {
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
                }

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

        // We assume that the formula is in QBF
        // (But no other assumption)
        public static bool Solve(FormulaCNF.QBCNFormula qbcnf)
        {
            // Now create a QDIMACS file and send it to 
            // a QBF solver
            CreateQDIMACS(qbcnf, tempFile);
            solverThread = new Process();
            solverThread.StartInfo.FileName = solverFile;
            solverThread.StartInfo.Arguments = tempFile;
            solverThread.StartInfo.RedirectStandardOutput = true;
            solverThread.StartInfo.RedirectStandardError = true;
            solverThread.StartInfo.UseShellExecute = false;
            solverThread.Start();
            solverThread.WaitForExit();
            if (solverThread.ExitCode != retSAT && solverThread.ExitCode != retUNSAT)
                throw new Exception("Unexpected exit code from solver: " + solverThread.ExitCode.ToString());
            return solverThread.ExitCode == retSAT;
        }

        public static void AbortSolver()
        {
            if (solverThread == null) return;
            if (!solverThread.HasExited) solverThread.Kill();
        }
    }
}
