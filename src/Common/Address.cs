using Chibikko.Common.Ir;

namespace Chibikko.Common;

public readonly record struct Address(uint Value) : IComparable<Address>
{
    public static Address operator +(Address address, int offset) => new((uint)(address.Value + offset));
    public static Address operator +(int offset, Address address) => address + offset;
    public static Address operator -(Address address, int offset) => new((uint)(address.Value - offset));
    public static int operator -(Address left, Address right) => (int)(left.Value - right.Value);

    public static Address operator ++(Address address) => address + 1;
    public static Address operator --(Address address) => address - 1;

    public static bool operator <(Address left, Address right) => left.Value < right.Value;
    public static bool operator >(Address left, Address right) => left.Value > right.Value;
    public static bool operator <=(Address left, Address right) => left.Value <= right.Value;
    public static bool operator >=(Address left, Address right) => left.Value >= right.Value;

    public int CompareTo(Address other) => Value.CompareTo(other.Value);

    public override string ToString() => $"0x{Value:X8}";

    public static IrType ToIrType() => IrType.I32;
}
