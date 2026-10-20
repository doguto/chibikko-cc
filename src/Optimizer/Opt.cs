using Chibikko.Common.Ir;

namespace Chibikko.Optimizer;

public interface IPass
{
    void Run(Module module);
}

public sealed class PassManager
{
    private readonly List<IPass> _passes = [];

    public void Add(IPass pass) => _passes.Add(pass);

    public Module Run(Module module)
    {
        foreach (var pass in _passes)
        {
            pass.Run(module);
        }
        return module;
    }
}
