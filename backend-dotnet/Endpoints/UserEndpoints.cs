using backend_dotnet.Models;
using backend_dotnet.Services.User;
using System.Net;

namespace backend_dotnet.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var userGroup = app.MapGroup("/api/user").WithTags("User");

            userGroup.MapGet("/GetRegisteredUserCount", GetRegisteredUserCount)
                .WithName("GetRegisteredUserCount")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> GetRegisteredUserCount(IUserService userService)
        {
            try
            {
                var result = await userService.GetRegisteredUserCountAsync();

                if (!result.IsSuccess)
                {
                    Console.Error.WriteLine($"getRegisteredUserCount error: {result.ErrorMessage}");
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new { totalUsers = result.TotalUsers }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetRegisteredUserCount error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
