using InstaPatch;
using InstaPatch.Helpers;

namespace InstaPatch.Tests;

public class PreventPatchAttributeTests
{
    [Fact]
    public void Validate_ReturnsError_WhenClassHasPreventPatchAttribute()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Replace,
            Path = "/property1",
            Value = "value"
        };

        var results = PatchDoc<PreventPatchClass>.Validate([operation]).ToArray();
        results.ShouldNotBeNull();
        results.Length.ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(ErrorMessages<PreventPatchClass>.TypeNotPatchable, nameof(PreventPatchClass)));

        PatchDoc<PreventPatchClass>.IsValid([operation]).ShouldBeFalse();
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
    public void Validate_ReturnsError_WhenAttemptingToPatchPreventPatchProperty()
    {
        var operationType = OperationType.Replace;
        var operationPath = "/property2";

        var operations = new List<PatchOperation>
        {
            new() {
                Op = OperationType.Replace,
                Path = "/property1",
                Value = "value"
            },
            new() {
                Op = operationType,
                Path = operationPath,
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
        result.ErrorMessage.ShouldBe(string.Format(ErrorMessages<AllowPartialPatch>.OperationPathNotValid, operationType, operationPath));

        PatchDoc<AllowPartialPatch>.IsValid(operations).ShouldBeFalse();
    }
}


[PreventPatch]
public class PreventPatchClass
{
    public string Property1 { get; set; } = null!;

    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}

public class AllowPartialPatch
{
    public string Property1 { get; set; } = null!;

    [PreventPatch]
    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}

public class AllowPatch
{
    public string Property1 { get; set; } = null!;

    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;
}
