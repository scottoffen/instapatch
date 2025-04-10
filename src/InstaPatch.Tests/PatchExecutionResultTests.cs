namespace InstaPatch.Tests;

public class PatchExecutionResultTests
{
    [Fact]
    public void Constructor_WithoutErrorMessage_SetsSuccessToTrue()
    {
        var result = new PatchExecutionResult(new PatchOperation());
        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithErrorMessage_SetsSuccessToFalse()
    {
        var error = Guid.NewGuid().ToString();

        var result = new PatchExecutionResult(new PatchOperation(), error);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(error);
    }
}
