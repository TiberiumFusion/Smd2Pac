using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class Program
    {
        static void Main(string[] args)
        {
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
            SmdData smdData = null;
            try
            {
                smdData = SmdData.FromFile(launchArgs.SourceSmdFilePath);
            }
            catch (Exception e) when (e is IOException || e is FileNotFoundException || e is DirectoryNotFoundException || e is PathTooLongException || e is ArgumentException)
            {
                Print("[!!!] Error reading SMD file [!!!]");
                Print(e.Message, 1, "- ");
            }
            catch (Exception e)
            {
                Print("[!!!] Error parsing SMD file [!!!]");
                Print(e.Message, 1, "- ");
            }

            ///// Translation to PAC3 animation 
            PacCustomAnimation pacCustomAnim = null;
            try
            {
                pacCustomAnim = PacCustomAnimation.FromSmdData(smdData);
            }
            catch (Exception e)
            {
                Print("[!!!] Error translating SMD sequence to PAC3 custom animation [!!!]");
                Print(e.Message, 1, "- ");
            }

            ///// Write output
            try
            {
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.FloatParseHandling = FloatParseHandling.Double;
                serializerSettings.FloatFormatHandling = FloatFormatHandling.String;
                serializerSettings.Formatting = Formatting.None;
                serializerSettings.Converters.Add(new NoScientificNotationBS());
                string pacAnimInterchange = JsonConvert.SerializeObject(pacCustomAnim, serializerSettings);
                File.WriteAllText(launchArgs.OutputPacAnimDataPath, pacAnimInterchange);
            }
            catch (Exception e)
            {
                Print("[!!!] Error writing PAC3 custom animation data to file [!!!]");
                Print(e.Message, 1, "- ");
            }

            Console.ReadKey();
        }

        private static void Print(string message, int indentLevel = 0, string bullet = null)
        {
            foreach (string line in message.Replace("\r\n", "\n").Split('\n'))
                Console.WriteLine(string.Concat(Enumerable.Repeat("    ", indentLevel)) + (bullet ?? "") + line);
        }
    }
}
