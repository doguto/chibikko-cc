using Chibikko.Common.Ir;

namespace Chibikko;

public interface IFrontendPipeline
{
    Module Run(string source);
}
