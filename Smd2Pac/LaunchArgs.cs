﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class LaunchArgs
    {
        public string SourceSmdFilePath { get; private set; } = null;
        public string OutputPacAnimDataPath { get; private set; } = null;
        public List<string> IgnoreBones { get; private set; } = new List<string>();

        private class ArgDef
        {
            internal int RequiresValue = 0;
            internal string Example = "";
            internal ArgDef(int requiresValue, string example)
            {
                RequiresValue = requiresValue;
                Example = example;
            }
        }
        private static Dictionary<string, ArgDef> ValidArgs = new Dictionary<string, ArgDef>()
        {
            ["smd"] = new ArgDef(1, "--smd \"path\\to\\my file.smd\""),
            ["output"] = new ArgDef(1, "--output \"path\\to\\my new pac3 anim data.txt\""),
            ["ignore-bones"] = new ArgDef(-1, "--ignore-bones ValveBiped.Bip01_L_Thigh ValveBiped.Bip01_L_Calf")
        };

        private static string StringIsArgKeyword(string s)
        {
            foreach (string argKeyword in ValidArgs.Keys)
                if (s == "-" + argKeyword || s == "--" + argKeyword)
                    return argKeyword;
            return null;
        }

        private const string ExceptionMessageNoSmd = "You must specify an input file, either as the very first or last argument, or with the --smd argument.\nYou must use the --smd argument if you also are using --ignore-bones.\nExample: smd2pac.exe \"\\path\\to\\some anims.smd\"\nExample: smd2pac.exe --smd \"path\\to\\some other anims.smd\"";

        public LaunchArgs(string[] rawArgs)
        {
            if (rawArgs.Length == 0)
                throw new Exception(ExceptionMessageNoSmd);

            List<string> parseArgs = rawArgs.ToList();

            // Drap and drop filename arg check
            bool firstArgIsAKeyword = StringIsArgKeyword(parseArgs[0].ToLowerInvariant()) != null;
            if (!firstArgIsAKeyword)
            {
                SourceSmdFilePath = parseArgs[0];
                parseArgs.RemoveAt(0);
            }
            
            // Check the rest like usual
            for (int i = 0; i < parseArgs.Count; i++)
            {
                string userArg = parseArgs[i].ToLowerInvariant();

                string argKeyword = StringIsArgKeyword(userArg);
                List<string> argMultiValues = new List<string>();

                if (argKeyword != null)
                {
                    ArgDef argDef = ValidArgs[argKeyword];
                    if (argDef.RequiresValue != 0)
                    {
                        bool anyCount = argDef.RequiresValue == -1;
                        int t;
                        for (t = i + 1; t < parseArgs.Count; t++)
                        {
                            string nextArg = parseArgs[t];
                            if (StringIsArgKeyword(nextArg) != null)
                            {
                                if (anyCount)
                                    break;
                                else
                                {
                                    if (argMultiValues.Count < argDef.RequiresValue)
                                        throw new Exception("Invalid value for argument --" + argKeyword + ". Example usage: " + argDef.Example);
                                    else if (argMultiValues.Count > argDef.RequiresValue)
                                        throw new Exception("Too many values for argument --" + argKeyword + ". Example usage: " + argDef.Example);
                                }
                            }
                            
                            argMultiValues.Add(parseArgs[t]);

                            if (!anyCount && t == parseArgs.Count - 1 && argMultiValues.Count < argDef.RequiresValue)
                                throw new Exception("Insufficient number of values for argument --" + argKeyword + ". Example usage: " + argDef.Example);
                        }
                        i = t - 1;
                    }   
                }
                else
                {
                    if (i == parseArgs.Count - 1)
                        SourceSmdFilePath = userArg; // Assume the final arg is the smd file path if it's not following any arg keyword
                }

                if (argKeyword == "smd")
                {
                    SourceSmdFilePath = argMultiValues[0];
                }
                else if (argKeyword == "output")
                {
                    OutputPacAnimDataPath = argMultiValues[0];
                }
                else if (argKeyword == "ignore-bones")
                {
                    IgnoreBones.AddRange(argMultiValues);
                }
            }

            // Validate
            if (SourceSmdFilePath == null)
                throw new Exception(ExceptionMessageNoSmd);
            if (!File.Exists(SourceSmdFilePath))
                throw new Exception("File specified by --smd argument does not exist.");

            if (OutputPacAnimDataPath == null)
            {
                int extSpot = SourceSmdFilePath.LastIndexOf('.');
                OutputPacAnimDataPath = SourceSmdFilePath.Substring(0, extSpot) + "_pac3data.txt"; // Default output filename
            }
        }
    }
}
