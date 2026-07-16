namespace Backend.Shared;

public class ApiErrorResponse
{
    public int Status { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
