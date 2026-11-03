using Chibikko.Common;

namespace Chibikko;

public interface IFrontendPipeline
{
    Ir Run(string source);
}
