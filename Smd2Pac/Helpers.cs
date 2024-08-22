using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public static class Helpers
    {
        // https://stackoverflow.com/a/14655199/2489580
        // This seems to be as 'close' as it gets to a one-liner that isn't a hideous regex
        public static string[] SplitStringOnWhitespaceWithQuotes(string input)
        {
            return input.Split('"')
                        .Select((element, index) => index % 2 == 0  // If even index
                                               ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                               : new string[] { element })  // Keep the entire item
                        .SelectMany(element => element).ToArray();
        }
    }
}
