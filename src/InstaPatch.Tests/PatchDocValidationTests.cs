namespace InstaPatch.Tests;

public class PatchDocValidationTests
{
    [Theory]
    [InlineData(OperationType.Add)]
    [InlineData(OperationType.Copy)]
    [InlineData(OperationType.Move)]
    [InlineData(OperationType.Remove)]
    [InlineData(OperationType.Replace)]
    [InlineData(OperationType.Test)]
    public void Validate_ReturnsError_WhenPathIsMissing(OperationType op)
    {
        var operation = new PatchOperation
        {
            Op = op,
            Path = "/",
            Value = "value",
            From = "/property2"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessageOperationRequiresPath, operation.Op));
    }

    [Fact]
    public void Validate_ReturnsError_WhenPathIsNotReadable()
    {
        var operation = new PatchOperation
        {
            Op = OperationType.Test,
            Path = "/property0",
            Value = "value"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessagePropertyNotReadable, operation.Path));
    }

    [Theory]
    [InlineData(OperationType.Add)]
    [InlineData(OperationType.Copy)]
    [InlineData(OperationType.Remove)]
    [InlineData(OperationType.Replace)]
    public void Validate_ReturnsError_WhenPathIsNotWriteable(OperationType op)
    {
        var operation = new PatchOperation
        {
            Op = op,
            Path = "/property2",
            Value = "value"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBeGreaterThan(0);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessagePropertyNotWriteable, operation.Path));
    }

    [Theory]
    [InlineData(OperationType.Add)]
    [InlineData(OperationType.Replace)]
    public void Validate_ReturnsError_WhenRequiredValueIsMissing(OperationType op)
    {
        var operation = new PatchOperation
        {
            Op = op,
            Path = "/property1"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessageOperationRequiresValue, operation.Op));
    }

    [Theory]
    [InlineData(OperationType.Copy)]
    [InlineData(OperationType.Move)]
    public void Validate_ReturnsError_WhenRequiredFromIsMissing(OperationType op)
    {
        var operation = new PatchOperation
        {
            Op = op,
            Path = "/property1"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessageOperationRequiresFrom, operation.Op));
    }

    [Theory]
    [InlineData(OperationType.Copy)]
    [InlineData(OperationType.Move)]
    public void Validate_ReturnsError_WhenFromIsNotReadable(OperationType op)
    {
        var operation = new PatchOperation
        {
            Op = op,
            Path = "/property1",
            From = "/property0"
        };

        var results = PatchDoc<ValidateMe>.Validate([operation]);
        results.ShouldNotBeNull();
        results.Count().ShouldBe(1);

        var result = results.FirstOrDefault();
        result.ShouldNotBeNull();
        result.ErrorMessage.ShouldBe(string.Format(PatchDoc<ValidateMe>.ErrorMessagePropertyNotReadable, operation.From));
    }

    [Fact]
    public void Validate_ReturnsNoErrors_WhenAllOperationsAreValid()
    {
        var operations = new List<PatchOperation>
        {
            new()
            {
                Op = OperationType.Add,
                Path = "/property0",
                Value = "value",
            },
            new()
            {
                Op = OperationType.Copy,
                Path = "/property0",
                From = "/property2",
            },
            new()
            {
                Op = OperationType.Move,
                Path = "/property1",
                From = "/property2",
            },
            new()
            {
                Op = OperationType.Remove,
                Path = "/property1",
            },
            new()
            {
                Op = OperationType.Replace,
                Path = "/property1",
                Value = "value",
            },
            new()
            {
                Op = OperationType.Test,
                Path = "/property1",
                Value = "value",
            },
        };

        var results = PatchDoc<ValidateMe>.Validate(operations);
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
    }
}

public class ValidateMe
{
    private string _field0 = null!;

    /// <summary>
    /// This property can be written to but not read.
    /// </summary>
    public string Property0
    {
        set
        {
            _field0 = value;
        }
    }

    /// <summary>
    /// This property can be read and written to.
    /// </summary>
    public string Property1 { get; set; } = null!;

    /// <summary>
    /// This property can be read but not written to.
    /// </summary>
    public string Property2
    {
        get
        {
            return _field0;
        }
    }
}
