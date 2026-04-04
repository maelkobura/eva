using Eva.Commons.Security.Certificate;

namespace Eva.Node.Service.Calling;

/// <summary>
/// Interface for calling function from another node (or loopback)
/// </summary>
public interface IServiceRouter : IDisposable
{
    /// <summary>
    /// Call service function with explicit borrow certificate
    /// </summary>
    /// <param name="fullName">The full name of the function to call (e.g. "service.function_name").</param>
    /// <param name="cert">The borrow certificate to use for authentication.</param>
    /// <param name="parameters">The parameters to pass to the function.</param>
    /// <returns>The result of the function call.</returns>
    public Task<T> Call<T>(string fullName, Certificate cert, params object?[] parameters);

    /// <summary>
    /// Call service function with node certificate
    /// </summary>
    /// <param name="fullName">The full name of the function to call (e.g. "service.function_name").</param>
    /// <param name="parameters">The parameters to pass to the function.</param>
    /// <returns>The result of the function call.</returns>
    public Task<T> Call<T>(string fullName, params object?[] parameters);

}