using System;

namespace RoodFluweel.ValueObjects;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class ValueObjectAttribute : Attribute
{
}