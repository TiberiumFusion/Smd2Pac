using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public class NumberedLine
    {
        public string Text = null;
        public int LineNumber = -1;

        public NumberedLine(string text, int lineNumber)
        {
            Text = text;
            LineNumber = lineNumber;
        }

        public override string ToString()
        {
            return "(" + LineNumber + ") " + Text;
        }
    }
}
