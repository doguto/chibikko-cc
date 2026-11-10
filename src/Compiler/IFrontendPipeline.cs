using Chibikko.Common.Ir;
using Chibikko.Common;

namespace Chibikko;

public interface IFrontendPipeline
{
    Ir Run(string source);
    string DumpAst(string source);
}
