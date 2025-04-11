namespace InstaPatch.Tests;

public class PatchDocTryApplyTests
{
    [Fact]
    public void TryApplyTest_ReturnsTrueOrFalse_BasedOnComparison()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Test,
            Path = "/property1",
            Value = Guid.NewGuid()
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result1 = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions1);
        result1.ShouldBeFalse();
        executions1.Count().ShouldBe(1);
        executions1.First().Success.ShouldBeFalse();
        executions1.First().ErrorMessage.ShouldNotBeNull();

        operation.Value = model.Property1;
        var result2 = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions2);
        result2.ShouldBeTrue();
        executions2.Count().ShouldBe(1);
        executions2.First().Success.ShouldBeTrue();
        executions2.First().ErrorMessage.ShouldBeNull();

        model.Property1.ShouldBe(property1);
        model.Property2.ShouldBe(property2);
        model.Property3.ShouldBe(property3);
    }

    [Fact]
    public void TryApplyReplace_ReplacesValue()
    {
        var expected = Guid.NewGuid();
        var operation = new PatchOperation
        {
            Op = OperationType.Replace,
            Path = "/property1",
            Value = expected
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions);
        result.ShouldBeTrue();
        executions.Count().ShouldBe(1);
        executions.First().Success.ShouldBeTrue();
        executions.First().ErrorMessage.ShouldBeNull();

        model.Property1.ShouldBe(expected);
        model.Property2.ShouldBe(property2);
        model.Property3.ShouldBe(property3);
    }

    [Fact]
    public void TryApply_Fails_IfOperationFails()
    {
        var operations = new[]
        {
            new PatchOperation
            {
                Op = OperationType.Test,
                Path = "/property1",
                Value = Guid.NewGuid()
            },
            new PatchOperation
            {
                Op = OperationType.Replace,
                Path = "/property2",
                Value = Guid.NewGuid().ToString()
            }
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid(operations).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, operations, out var executions);
        result.ShouldBeFalse();
        executions.Count().ShouldBe(2);
        executions.First().Success.ShouldBeFalse();
        executions.Last().Success.ShouldBeTrue();

        model.Property1.ShouldBe(property1);
        model.Property2.ShouldBe(property2);
        model.Property3.ShouldBe(property3);
    }

    [Fact]
    public void TryApplyRemove_SetsPropertyToDefaultValue()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Remove,
            Path = "/property1"
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions);
        result.ShouldBeTrue();
        model.Property1.ShouldBe(default);
        model.Property1.ShouldNotBe(property1);
        model.Property2.ShouldBe(property2);
        model.Property3.ShouldBe(property3);
    }

    [Fact]
    public void TryApplyCopy_SetsPathToValueOfFrom()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Copy,
            From = "/property3",
            Path = "/property2"
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions);
        result.ShouldBeTrue();
        model.Property1.ShouldBe(property1);
        model.Property2.ShouldBe(property3);
        model.Property3.ShouldBe(property3);
    }

    [Fact]
    public void TryApplyMove_SetsPathToValueOfFrom()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Move,
            From = "/property3",
            Path = "/property2"
        };

        var property1 = Guid.NewGuid();
        var property2 = Guid.NewGuid().ToString();
        var property3 = Guid.NewGuid().ToString();

        var model = new ApplyChangesToMe
        {
            Property1 = property1,
            Property2 = property2,
            Property3 = property3
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions);
        result.ShouldBeTrue();
        model.Property1.ShouldBe(property1);
        model.Property2.ShouldBe(property3);
        model.Property3.ShouldBe(default);
    }

    [Fact]
    public void TryApplyMove_SetsPathToValueOfFrom_Arrays()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Move,
            From = "/stringArray1",
            Path = "/stringArray2"
        };

        var array1 = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
        var array2 = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        var model = new ApplyChangesToMe
        {
            Property1 = Guid.NewGuid(),
            Property2 = Guid.NewGuid().ToString(),
            Property3 = Guid.NewGuid().ToString(),
            StringArray1 = array1,
            StringArray2 = array2,
        };

        PatchDoc<ApplyChangesToMe>.IsValid([operation]).ShouldBeTrue();
        var result = PatchDoc<ApplyChangesToMe>.TryApplyPatch(model, [operation], out var executions);
        result.ShouldBeTrue();
        model.StringArray1.ShouldBe(default);
        model.StringArray2.ShouldBe(array1);
    }
}

public class ApplyChangesToMe
{
    public Guid Property1 { get; set; }

    public string Property2 { get; set; } = null!;

    public string Property3 { get; set; } = null!;

    public IEnumerable<string> StringArray1 { get; set; } = [];

    public IEnumerable<string> StringArray2 { get; set; } = [];
}
