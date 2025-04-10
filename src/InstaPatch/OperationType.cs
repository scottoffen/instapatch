namespace InstaPatch;

/// <summary>
/// Operation types based on the RFC 6902 specification.
/// </summary>
[Flags]
public enum OperationType
{
    /// <summary>
    /// The operation adds a value to the specified location.
    /// </summary>
    Add = 1 << 0,

    /// <summary>
    /// The operation copies a value from one location to another.
    /// </summary>
    Copy = 1 << 1,

    /// <summary>
    /// The operation moves a value from one location to another.
    /// </summary>
    Move = 1 << 2,

    /// <summary>
    /// The operation removes the value at the specified location.
    /// </summary>
    Remove = 1 << 3,

    /// <summary>
    /// The operation replaces the value at the specified location.
    /// </summary>
    Replace = 1 << 4,

    /// <summary>
    /// The operation tests that a value at the specified location is equal to a specified value.
    /// </summary>
    Test = 1 << 5,
}
