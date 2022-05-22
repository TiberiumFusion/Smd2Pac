using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class LaunchArgs
    {
        // ____________________________________________________________________________________________________
        //
        //   Argument values and specification
        // ____________________________________________________________________________________________________
        //

        public string SourceSmdFilePath { get; private set; } = null;
        public bool DeepSmdDirPath { get; private set; } = false;
        public string OutputPacAnimDataPath { get; private set; } = null;
        public bool EscapeOutputPacAnimData { get; private set; } = true;
        public int PacAnimDataOptimizeLevel { get; private set; } = 1;
        public List<string> IgnoreBones { get; private set; } = new List<string>();
        public Dictionary<string, Tuple<Vector3, Vector3>> BoneFixups { get; private set; } = new Dictionary<string, Tuple<Vector3, Vector3>>();
        public string SubtractionBaseSmd { get; private set; } = null;
        public int SubtractionBaseFrame { get; private set; } = 0;
        public bool DumpSubtractedSmd { get; private set; } = false;
        public bool HideWarnings { get; private set; } = false;
        public bool ParseWithSystemLocale { get; private set; } = false;
        public bool WriteWithSystemLocale { get; private set; } = false;

        public List<string> UnknownArguments { get; private set; } = new List<string>();
        
        private bool UsedSmdArg = false; // True when --smd is present (i.e. no implicit smd arg)

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
            ["deep"] = new ArgDef(0, "--deep"),
            ["output"] = new ArgDef(1, "--output \"path\\to\\my new pac3 anim data.txt\""),
            ["dont-escape-output"] = new ArgDef(0, "--dont-escape-output"),
            ["optimize"] = new ArgDef(1, "--optimize 1"),
            ["ignore-bones"] = new ArgDef(-1, "--ignore-bones ValveBiped.Bip01_L_Thigh ValveBiped.Bip01_L_Calf"),
            ["bone-fixup"] = new ArgDef(7, "--bone-fixup ValveBiped.Bip01_Pelvis 0 0 0 0 0 90"),
            ["make-additive-from"] = new ArgDef(1, "--make-additive-from \"path\\to\\reference pose.smd\""),
            ["additive-base-frame"] = new ArgDef(1, "--additive-base-frame 0"),
            ["dump-additive-smd"] = new ArgDef(0, "--dump-additive-smd"),
            ["hide-warnings"] = new ArgDef(0, "--hide-warnings"),
            ["parse-with-system-locale"] = new ArgDef(0, "--parse-with-system-locale"),
            ["write-with-system-locale"] = new ArgDef(0, "--write-with-system-locale"),
        };

        private static string StringIsArgKeyword(string s)
        {
            foreach (string argKeyword in ValidArgs.Keys)
                if (s == "--" + argKeyword)
                    return argKeyword;
            return null;
        }

        private const string ExceptionMessageNoSmd = "You must specify an input file, either as the very first or last argument, or with the --smd argument.\nYou must use the --smd argument if you also are using --ignore-bones.\nExample: smd2pac.exe \"\\path\\to\\some anims.smd\"\nExample: smd2pac.exe --smd \"path\\to\\some other anims.smd\"";


        // ____________________________________________________________________________________________________
        //
        //   Arguments parsing
        // ____________________________________________________________________________________________________
        //
        
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


            //
            // Parse all arguments
            //

            for (int i = 0; i < parseArgs.Count; i++)
            {
                bool rewindForNextArg = false;

                string userArg = parseArgs[i].ToLowerInvariant().Replace('‑', '-'); // Replace nonbreaking hyphen with hyphens (in case the user copy-pastes from this project's github wiki... because github doesn't know how to set the width of a table column in 2022...)

                string argKeyword = StringIsArgKeyword(userArg);
                List<string> argMultiValues = new List<string>();

                if (argKeyword != null)
                {
                    ArgDef argDef = ValidArgs[argKeyword];
                    if (argDef.RequiresValue != 0)
                    {
                        bool anyCount = (argDef.RequiresValue == -1);
                        int t;
                        for (t = i + 1; t < parseArgs.Count; t++)
                        {
                            string nextArg = parseArgs[t];
                            if (StringIsArgKeyword(nextArg) != null)
                            {
                                if (anyCount)
                                {
                                    rewindForNextArg = true;
                                    break;
                                }
                                else
                                {
                                    if (argMultiValues.Count < argDef.RequiresValue)
                                        throw new Exception("Not enough values for argument --" + argKeyword + ".\n  Example usage: " + argDef.Example);
                                    else if (argMultiValues.Count > argDef.RequiresValue)
                                        throw new Exception("Too many values for argument --" + argKeyword + ".\n  Example usage: " + argDef.Example);
                                }
                            }
                            
                            argMultiValues.Add(parseArgs[t]);
                            if (argMultiValues.Count == argDef.RequiresValue)
                                break;

                            if (!anyCount && t == parseArgs.Count - 1 && argMultiValues.Count < argDef.RequiresValue)
                                throw new Exception("Insufficient number of values for argument --" + argKeyword + ".\n  Example usage: " + argDef.Example);
                        }
                        i = t;
                        
                        if (argMultiValues.Count < argDef.RequiresValue)
                            throw new Exception("Not enough values for argument --" + argKeyword + ".\n  Example usage: " + argDef.Example);
                    }   
                }
                else
                {
                    if (i == parseArgs.Count - 1 && (userArg.Length >= 2 && userArg.Substring(0, 2) != "--") && !UsedSmdArg)
                        SourceSmdFilePath = userArg; // Assume this final arg is the smd file path
                    else
                        UnknownArguments.Add(userArg); // Unknown/invalid argument
                }

                if (argKeyword == "smd")
                {
                    UsedSmdArg = true;
                    SourceSmdFilePath = argMultiValues[0];
                }
                else if (argKeyword == "deep")
                {
                    DeepSmdDirPath = true;
                }
                else if (argKeyword == "output")
                {
                    OutputPacAnimDataPath = argMultiValues[0];
                }
                else if (argKeyword == "dont-escape-output")
                {
                    EscapeOutputPacAnimData = false;
                }
                else if (argKeyword == "optimize")
                {
                    int optimizeLevel = 0;
                    if (!int.TryParse(argMultiValues[0], out optimizeLevel))
                        throw new Exception("Invalid value for argument --" + argKeyword + " (not an integer).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    PacAnimDataOptimizeLevel = optimizeLevel;
                }
                else if (argKeyword == "ignore-bones")
                {
                    if (argMultiValues.Count > 0)
                        IgnoreBones.AddRange(argMultiValues);
                }
                else if (argKeyword == "bone-fixup")
                {
                    float posX = 0f;
                    float posY = 0f;
                    float posZ = 0f;
                    float rotX = 0f;
                    float rotY = 0f;
                    float rotZ = 0f;
                    if (!float.TryParse(argMultiValues[1], out posX))
                        throw new Exception("Invalid X translation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (!float.TryParse(argMultiValues[2], out posY))
                        throw new Exception("Invalid Y translation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (!float.TryParse(argMultiValues[3], out posZ))
                        throw new Exception("Invalid Z translation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (!float.TryParse(argMultiValues[4], out rotX))
                        throw new Exception("Invalid X rotation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (!float.TryParse(argMultiValues[5], out rotY))
                        throw new Exception("Invalid Y rotation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (!float.TryParse(argMultiValues[6], out rotZ))
                        throw new Exception("Invalid Z rotation for argument --" + argKeyword + " (not a number).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    BoneFixups[argMultiValues[0]] = new Tuple<Vector3, Vector3>(new Vector3(posX, posY, posZ), new Vector3(rotX, rotY, rotZ));
                }
                else if (argKeyword == "make-additive-from")
                {
                    if (!File.Exists(argMultiValues[0]))
                        throw new Exception("File specified by argument --" + argKeyword + " does not exist.\n  Example usage: " + ValidArgs[argKeyword].Example);
                    SubtractionBaseSmd = argMultiValues[0];
                }
                else if (argKeyword == "additive-base-frame")
                {
                    int frame = 0;
                    if (!int.TryParse(argMultiValues[0], out frame))
                        throw new Exception("Invalid value for argument --" + argKeyword + " (not an integer).\n  Example usage: " + ValidArgs[argKeyword].Example);
                    if (frame < 0)
                        throw new Exception("Invalid value for argument --" + argKeyword + ", value cannot be less than 0.\n  Example usage: " + ValidArgs[argKeyword].Example);
                    SubtractionBaseFrame = frame;
                }
                else if (argKeyword == "dump-additive-smd")
                {
                    DumpSubtractedSmd = true;
                }
                else if (argKeyword == "hide-warnings")
                {
                    HideWarnings = true;
                }
                else if (argKeyword == "parse-with-system-locale")
                {
                    ParseWithSystemLocale = true;
                }
                else if (argKeyword == "write-with-system-locale")
                {
                    WriteWithSystemLocale = true;
                }

                if (rewindForNextArg)
                    i--;
            }


            //
            // Defaults and final validation
            //

            // Smd file
            if (SourceSmdFilePath == null)
                throw new Exception(ExceptionMessageNoSmd);
            if (!File.Exists(SourceSmdFilePath) && !Directory.Exists(SourceSmdFilePath))
                throw new Exception("Invalid --smd path: \"" + SourceSmdFilePath + "\". No such file or directory exists.");
        }
    }
}
