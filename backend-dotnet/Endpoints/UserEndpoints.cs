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

            userGroup.MapGet("/profile", GetPatientProfile)
                .WithName("GetPatientProfile")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .RequireAuthorization();

            userGroup.MapPut("/profile", UpdatePatientProfile)
                .WithName("UpdatePatientProfile")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .RequireAuthorization();

            userGroup.MapGet("/admin-profile", GetAdminProfile)
                .WithName("GetAdminProfile")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .RequireAuthorization();

            userGroup.MapPut("/admin-profile", UpdateAdminProfile)
                .WithName("UpdateAdminProfile")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .RequireAuthorization();
        }


        //-------------------------------GetRegisteredUserCount------------------------------------------------------

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
                    Result = new
                    {
                        totalUsers = result.TotalUsers,
                        totalPatients = result.TotalPatients,
                        totalAdmins = result.TotalAdmins
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetRegisteredUserCount error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        //-------------------------------GetPatientProfile------------------------------------------------------

        private static async Task<IResult> GetPatientProfile(IUserService userService, System.Security.Claims.ClaimsPrincipal user)
        {
            try
            {
                var authenticatedUserIdStr = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserIdStr) || !Guid.TryParse(authenticatedUserIdStr, out var authenticatedUserId))
                    return Results.Unauthorized();

                var result = await userService.GetPatientProfileAsync(authenticatedUserId);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = new List<string> { result.ErrorMessage ?? "Error" }
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Profile
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetPatientProfile error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------UpdatePatientProfile------------------------------------------------------

        private static async Task<IResult> UpdatePatientProfile(
            IUserService userService,
            backend_dotnet.Models.DTOs.User.UpdatePatientProfileRequestDTO request,
            System.Security.Claims.ClaimsPrincipal user)
        {
            try
            {
                var authenticatedUserIdStr = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserIdStr) || !Guid.TryParse(authenticatedUserIdStr, out var authenticatedUserId))
                    return Results.Unauthorized();

                var result = await userService.UpdatePatientProfileAsync(authenticatedUserId, request);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = new List<string> { result.ErrorMessage ?? "Error" }
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Profile
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UpdatePatientProfile error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetAdminProfile------------------------------------------------------

        private static async Task<IResult> GetAdminProfile(IUserService userService, System.Security.Claims.ClaimsPrincipal user)
        {
            try
            {
                var authenticatedUserIdStr = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserIdStr) || !Guid.TryParse(authenticatedUserIdStr, out var authenticatedUserId))
                    return Results.Unauthorized();

                var result = await userService.GetAdminProfileAsync(authenticatedUserId);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = new List<string> { result.ErrorMessage ?? "Error" }
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Profile
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetAdminProfile error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------UpdateAdminProfile------------------------------------------------------

        private static async Task<IResult> UpdateAdminProfile(
            IUserService userService,
            backend_dotnet.Models.DTOs.User.UpdateAdminProfileRequestDTO request,
            System.Security.Claims.ClaimsPrincipal user)
        {
            try
            {
                var authenticatedUserIdStr = user.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(authenticatedUserIdStr) || !Guid.TryParse(authenticatedUserIdStr, out var authenticatedUserId))
                    return Results.Unauthorized();

                var result = await userService.UpdateAdminProfileAsync(authenticatedUserId, request);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = new List<string> { result.ErrorMessage ?? "Error" }
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Profile
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UpdateAdminProfile error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
