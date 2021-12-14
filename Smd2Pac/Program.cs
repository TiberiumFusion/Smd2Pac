using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TiberiumFusion.Smd2Pac
{
    public class Program
    {
        static void Main(string[] args)
        {
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            Print("\n-[-----     Smd2Pac " + ver.ToString() + "     -----]-");

            ///// Input reading
            LaunchArgs launchArgs = null;
            try
            {
                launchArgs = new LaunchArgs(args);
            }
            catch (Exception e)
            {
                Print("[!!!] Invalid arguments [!!!]");
                Print(e.Message, 1, "- ");
                return;
            }

            ///// Base SMD pose parsing
            SmdData basePoseSmdData = null;
            FileInfo basePoseSmdFile = null;
            if (launchArgs.SubtractionBaseSmd != null)
            {
                Print("Reading subtraction base pose SMD file: \"" + launchArgs.SubtractionBaseSmd + "\"");
                try
                {
                    basePoseSmdData = SmdData.FromFile(launchArgs.SubtractionBaseSmd);
                    basePoseSmdFile = new FileInfo(launchArgs.SubtractionBaseSmd);
                }
                catch (Exception e) when (e is IOException || e is FileNotFoundException || e is DirectoryNotFoundException || e is PathTooLongException || e is ArgumentException)
                {
                    Print("[!!!] Error reading SMD file [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }
                catch (Exception e)
                {
                    Print("[!!!] Error parsing SMD file [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }
            }

            ///// Find SMD files to process
            List<string> smdFiles = new List<string>();
            if (Directory.Exists(launchArgs.SourceSmdFilePath))
            {
                DirectoryInfo smdRootDir = new DirectoryInfo(launchArgs.SourceSmdFilePath);
                foreach (FileInfo smdFile in smdRootDir.GetFiles("*.smd", launchArgs.DeepSmdDirPath ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    if (smdFile.Length > 0 && (basePoseSmdFile == null || smdFile.FullName != basePoseSmdFile.FullName))
                        smdFiles.Add(smdFile.FullName.Substring(smdRootDir.FullName.Length + 1));
            }
            else if (File.Exists(launchArgs.SourceSmdFilePath))
                smdFiles.Add(launchArgs.SourceSmdFilePath);

            Print("\nProcessing " + smdFiles.Count + " SMD files...");

            for (int i = 0; i < smdFiles.Count; i++)
            {
                string smdFilename = smdFiles[i];

                ///// SMD parsing
                Print("\nReading SMD file: \"" + smdFilename + "\" (" + (i + 1) + "/" + smdFiles.Count + ")");
                SmdData smdData = null;
                try
                {
                    smdData = SmdData.FromFile(smdFilename);
                }
                catch (Exception e) when (e is IOException || e is FileNotFoundException || e is DirectoryNotFoundException || e is PathTooLongException || e is ArgumentException)
                {
                    Print("[!!!] Error reading SMD file [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }
                catch (Exception e)
                {
                    Print("[!!!] Error parsing SMD file [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }

                ///// Translation to PAC3 animation
                Print("Creating PAC3 animation data");
                PacCustomAnimation pacCustomAnim = null;
                SmdData subtractedSmdData = null;
                try
                {
                    pacCustomAnim = Translator.Smd2Pac(smdData,
                                                       launchArgs.IgnoreBones,
                                                       launchArgs.PacAnimDataOptimizeLevel,
                                                       launchArgs.BoneFixups,
                                                       basePoseSmdData,
                                                       launchArgs.SubtractionBaseFrame,
                                                       launchArgs.HideWarnings,
                                                       out subtractedSmdData);
                }
                catch (Exception e)
                {
                    Print("[!!!] Error translating SMD sequence to PAC3 custom animation [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }

                ///// Subtracted SMD data dump to file
                if (launchArgs.DumpSubtractedSmd && subtractedSmdData != null)
                {
                    string outputDir = Path.Combine(Path.GetDirectoryName(smdFilename), "subtracted smds");
                    Directory.CreateDirectory(outputDir);
                    string subtractedSmdDumpFilename = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(smdData.SourceFilename) + "_subtracted.smd");
                    Print("Dumping subtracted SMD data to \"" + subtractedSmdDumpFilename + "\"");
                    try
                    {
                        File.WriteAllLines(subtractedSmdDumpFilename, subtractedSmdData.ToLines(), Encoding.ASCII);
                    }
                    catch (Exception e)
                    {
                        Print("[!!!] Error writing subtracted SMD file [!!!]");
                        Print(e.Message, 1, "- ");
                    }
                }

                ///// Write PAC3 anim data interchange json output
                string outputFilename = launchArgs.OutputPacAnimDataPath;
                if (outputFilename == null)
                {
                    // Default output path
                    string outputDir = Path.Combine(Path.GetDirectoryName(smdFilename), "output");
                    Directory.CreateDirectory(outputDir);
                    outputFilename = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(smdFilename) + "_pac3animdata.txt");
                }
                Print("Writing PAC3 animation data to \"" + outputFilename + "\"");
                try
                {
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.FloatParseHandling = FloatParseHandling.Double;
                    serializerSettings.FloatFormatHandling = FloatFormatHandling.String;
                    serializerSettings.Formatting = Formatting.None;
                    serializerSettings.Converters.Add(new NoScientificNotationBS());
                    string pacAnimJson = JsonConvert.SerializeObject(pacCustomAnim, serializerSettings);

                    if (launchArgs.EscapeOutputPacAnimData)
                        pacAnimJson = HttpUtility.JavaScriptStringEncode(pacAnimJson, false); // Since PAC3 outfits are json themself, the json anim data itself must be escaped (quotes, mainly)

                    File.WriteAllText(outputFilename, pacAnimJson);
                    Print("File complete.");
                }
                catch (Exception e)
                {
                    Print("[!!!] Error writing PAC3 animation data to file [!!!]");
                    Print(e.Message, 1, "- ");
                    return;
                }
            }
            
            return;
        }

        internal static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            foreach (string line in message.Replace("\r\n", "\n").Split('\n'))
                Console.WriteLine(string.Concat(Enumerable.Repeat("    ", indentLevel)) + (bullet ?? "") + line);
        }
    }
}
