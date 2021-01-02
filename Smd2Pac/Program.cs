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

            ///// SMD parsing
            Print("Reading SMD file: \"" + launchArgs.SourceSmdFilePath + "\"");
            SmdData smdData = null;
            try
            {
                smdData = SmdData.FromFile(launchArgs.SourceSmdFilePath);
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

            ///// Base SMD pose parsing
            SmdData basePoseSmdData = null;
            if (launchArgs.SubtractionBaseSmd != null)
            {
                Print("Reading subtraction base pose SMD file: \"" + launchArgs.SubtractionBaseSmd + "\"");
                try
                {
                    basePoseSmdData = SmdData.FromFile(launchArgs.SubtractionBaseSmd);
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
                string subtractedSmdDumpFilename = Path.GetFileNameWithoutExtension(smdData.SourceFilename) + "_subtracted.smd";
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
            Print("Writing PAC3 animation data to \"" + launchArgs.OutputPacAnimDataPath + "\"");
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

                File.WriteAllText(launchArgs.OutputPacAnimDataPath, pacAnimJson);
                Print("File complete.");
            }
            catch (Exception e)
            {
                Print("[!!!] Error writing PAC3 animation data to file [!!!]");
                Print(e.Message, 1, "- ");
                return;
            }

            Console.ReadKey();
            
            return;
        }

        internal static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            foreach (string line in message.Replace("\r\n", "\n").Split('\n'))
                Console.WriteLine(string.Concat(Enumerable.Repeat("    ", indentLevel)) + (bullet ?? "") + line);
        }
    }
}
