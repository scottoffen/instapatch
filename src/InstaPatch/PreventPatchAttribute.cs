using System.Diagnostics.CodeAnalysis;

namespace InstaPatch;

/// <summary>
/// Attribute to mark properties or classes that should not be patched.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class PreventPatchAttribute : Attribute
{

}
