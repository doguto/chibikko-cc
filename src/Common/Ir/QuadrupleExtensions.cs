namespace Chibikko.Common.Ir;

public static class QuadrupleExtensions
{
    public static IEnumerable<Variable> Uses(this Quadruple quadruple)
    {
        if (quadruple.Source1 is Variable source1) yield return source1;
        if (quadruple.Source2 is Variable source2) yield return source2;
    }

    public static IEnumerable<Variable> Defs(this Quadruple quadruple)
    {
        if (quadruple.Destination is Variable destination) yield return destination;
    }

    public static IEnumerable<Variable> Uses(this Terminator terminator)
    {
        var operand = terminator switch
        {
            Branch branch => branch.Condition,
            Return @return => @return.Value,
            _ => null,
        };
        if (operand is Variable variable) yield return variable;
    }
}
