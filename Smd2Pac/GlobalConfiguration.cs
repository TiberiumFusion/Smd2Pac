using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TiberiumFusion.Smd2Pac
{
    public static class GlobalConfiguration
    {
        //
        // Decimal string handling
        //

        // Use the system's locale for parsing/writing decimal strings, such as the decimals in SMD files
        // Notably, this affects decimal syntax, i.e. 1,234,456.789 vs 1.234.456.789,0
        // As far as I know, only North American decimals are valid in SMD, but oddball tools may deviate from this
        public static bool ParseNumericStringsWithSystemLocale { get; set; } = false;
        public static bool WriteNumericStringsWithSystemLocale { get; set; } = false;

        // Conveniences for getting CultureInfos for parsing & writing decimals per the setting of ParseStringsWithSystemLocale
        // When not using the system's locale, we default to the invariant culture
        public static CultureInfo DecimalStringParsingCulture
        {
            get
            {
                if (ParseNumericStringsWithSystemLocale)
                    return CultureInfo.CurrentCulture;
                else
                    return CultureInfo.InvariantCulture;
            }
        }
        public static CultureInfo DecimalStringWritingCulture
        {
            get
            {
                if (WriteNumericStringsWithSystemLocale)
                    return CultureInfo.CurrentCulture;
                else
                    return CultureInfo.InvariantCulture;
            }
        }
    }
}
