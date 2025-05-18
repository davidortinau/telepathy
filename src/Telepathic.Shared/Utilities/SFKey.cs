using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telepathic.Shared.Utilities;

public static class SFKey
{
    private const string SFKeyValue = "REDACTED_SECRET";
    
    public static string GetSFKeyValue()
    {
        return SFKeyValue;
    }
}
