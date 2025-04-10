using InstaPatch;

namespace InstaPatch.Tests;

public class DenyPatchAttributeTests
{
    [Fact]
    public void Validate_ReturnsError_WhenClassHasDenyPatchAttribute()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Replace,
            Path = "/property1",
            Value = "value"
        };

        var results = PatchDoc<DenyPatchClass>.Validate([operation]).ToArray();
        results.ShouldNotBeNull();
        results.Length.ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<DenyPatchClass>.ErrorMessageTypeNotPatchable, nameof(DenyPatchClass)));

        PatchDoc<DenyPatchClass>.IsValid([operation]).ShouldBeFalse();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenClassCanBePatched()
    {
        var operations = new List<PatchOperation>
        {
            new() {
                Op = OperationType.Replace,
                Path = "/property1",
                Value = "value"
            },
            new() {
                Op = OperationType.Replace,
                Path = "/property2",
                Value = "value"
            },
            new() {
                Op = OperationType.Replace,
                Path = "/property3",
                Value = "value"
            }
        };

        var results = PatchDoc<AllowPatch>.Validate(operations).ToArray();
        results.ShouldNotBeNull();
        results.Length.ShouldBe(0);

        PatchDoc<AllowPatch>.IsValid(operations).ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsNoError_WhenClassCanBePartiallyPatched()
    {
        var operations = new List<PatchOperation>
        {
            new() {
                Op = OperationType.Replace,
                Path = "/property1",
                Value = "value"
            },
            new() {
                Op = OperationType.Replace,
                Path = "/property3",
                Value = "value"
            }
        };

        var results = PatchDoc<AllowPartialPatch>.Validate(operations).ToArray();
        results.ShouldNotBeNull();
        results.Length.ShouldBe(0);

        PatchDoc<AllowPartialPatch>.IsValid(operations).ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsError_WhenAttemptingToPatchDenyPatchProperty()
    {
        var operations = new List<PatchOperation>
        {
            new() {
                Op = OperationType.Replace,
                Path = "/property1",
                Value = "value"
            },
            new() {
                Op = OperationType.Replace,
                Path = "/property2",
                Value = "value"
            },
            new() {
                Op = OperationType.Replace,
                Path = "/property3",
                Value = "value"
            }
        };

        var results = PatchDoc<AllowPartialPatch>.Validate(operations).ToArray();
        results.ShouldNotBeNull();
        results.Length.ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<AllowPartialPatch>.ErrorMessagePropertyNotWriteable, "/property2"));

        PatchDoc<AllowPartialPatch>.IsValid(operations).ShouldBeFalse();
    }
}


[DenyPatch]
public class DenyPatchClass
{
    public string Property1 { get; set; } = null!;

    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}

public class AllowPartialPatch
{
    public string Property1 { get; set; } = null!;

    [DenyPatch]
    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}

public class AllowPatch
{
    public string Property1 { get; set; } = null!;

    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}
