using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services.Doctor;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_dotnetWebMinimalExample.Endpoints.Doctor
{
    public static class DoctorEndpoints
    {
        public static void MapDoctorEndpoints(this IEndpointRouteBuilder app)
        {
            var doctorGroup = app.MapGroup("/api/doctors").WithTags("Doctors").DisableAntiforgery();

            doctorGroup.MapPost("/CreateDoctor", CreateDoctor)
                 .WithName("CreateDoctor")
                 .Produces<ApiResponse>(StatusCodes.Status201Created)
                 .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                 .Produces<ApiResponse>(StatusCodes.Status409Conflict)
                 .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapPost("/login", LoginDoctor)
                 .WithName("LoginDoctor")
                 .Produces<ApiResponse>(StatusCodes.Status200OK)
                 .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                 .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                 .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapGet("/GetDoctors", GetDoctors)
                .WithName("GetDoctors")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapGet("/GetDoctorById/{id}", GetDoctorById)
                .WithName("GetDoctorById")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapPut("/UpdateDoctor/{id}", UpdateDoctor)
                .RequireAuthorization()
                .WithName("UpdateDoctor")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status409Conflict)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapDelete("/DeleteDoctor/{id}", DeleteDoctor)
                .RequireAuthorization()
                .WithName("DeleteDoctor")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            doctorGroup.MapPatch("/ToggleAvailability/{id}", ToggleAvailability)
                .RequireAuthorization()
                .WithName("ToggleAvailability")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);


        }

        //-------------------------------------------CreateDoctor-----------------------------------------------------

        private static async Task<IResult> CreateDoctor(
            [FromForm] CreateDoctorRequestDTO createDoctorRequestDTO,
            IFormFile? image,
            IDoctorService doctorService)
        {
            var result = await doctorService.CreateDoctorAsync(createDoctorRequestDTO, image);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.Conflict(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessages = [result.ErrorMessage]
                    });
                }

                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = [result.ErrorMessage ?? "Doctor registration failed"]
                });
            }

            return Results.Created("/api/doctors/CreateDoctor", new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.Created,
                Result = new
                {
                    token = result.Token,
                    data = result.Data
                }
            });
        }


        //-------------------------------------------LoginDoctor-----------------------------------------------------

        private static async Task<IResult> LoginDoctor(
            DoctorLoginRequestDTO request,
            IDoctorService doctorService)
        {
            var result = await doctorService.LoginDoctorAsync(request);

            if (!result.IsSuccess)
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = [result.ErrorMessage ?? "Login failed"]
                });
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new
                {
                    token = result.Token,
                    data = result.Data
                }
            });
        }

        //-------------------------------------------GetDoctors-----------------------------------------------------


        private static async Task<IResult> GetDoctors(
        [AsParameters] GetDoctorsRequestDTO getDoctorsRequestDTO,
        IDoctorService doctorService)
        {
            var result = await doctorService.GetDoctorsAsync(getDoctorsRequestDTO);

            if (!result.IsSuccess)
            {
                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to fetch doctors"]
                }, statusCode: (int)HttpStatusCode.InternalServerError);
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new
                {
                    data = result.Data,
                    meta = new { page = result.Page, limit = result.Limit, total = result.Total }
                }
            });
        }


        //-------------------------------------------GetDoctorById-----------------------------------------------------
        private static async Task<IResult> GetDoctorById(
        string id,
        IDoctorService doctorService)
        {
            var result = await doctorService.GetDoctorByIdAsync(id);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError;

                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to fetch doctor"]
                }, statusCode: (int)statusCode);
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new { data = result.Data }
            });
        }
        //-------------------------------------------UpdateDoctor-----------------------------------------------------
        private static async Task<IResult> UpdateDoctor(
        string id,
        [FromForm] UpdateDoctorRequestDTO updateDoctorRequestDTO,
        IFormFile? image,
        ClaimsPrincipal user,
        IDoctorService doctorService)
        {
            var authenticatedDoctorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(authenticatedDoctorId) || authenticatedDoctorId != id)
            {
                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Forbidden,
                    ErrorMessages = ["Not authorized to update this doctor"]
                }, statusCode: (int)HttpStatusCode.Forbidden);
            }

            var result = await doctorService.UpdateDoctorAsync(id, updateDoctorRequestDTO, image);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                    ? HttpStatusCode.NotFound
                    : result.ErrorMessage?.Contains("already in use", StringComparison.OrdinalIgnoreCase) == true
                        ? HttpStatusCode.Conflict
                        : HttpStatusCode.BadRequest;

                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessages = [result.ErrorMessage ?? "Doctor update failed"]
                }, statusCode: (int)statusCode);
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new { data = result.Data }
            });
        }

        //-------------------------------------------DeleteDoctor-----------------------------------------------------


        private static async Task<IResult> DeleteDoctor(
            string id,
            IDoctorService doctorService)
        {
            var result = await doctorService.DeleteDoctorAsync(id);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError;

                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to delete doctor"]
                }, statusCode: (int)statusCode);
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new { message = "Doctor removed" }
            });
        }

        //-------------------------------------------UpdateDoctor-----------------------------------------------------
        private static async Task<IResult> ToggleAvailability(
        string id,
        ClaimsPrincipal user,
        IDoctorService doctorService)
        {
            var authenticatedDoctorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(authenticatedDoctorId) || authenticatedDoctorId != id)
            {
                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Forbidden,
                    ErrorMessages = ["Not authorized to change availability for this doctor"]
                }, statusCode: (int)HttpStatusCode.Forbidden);
            }

            var result = await doctorService.ToggleAvailabilityAsync(id);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                    ? HttpStatusCode.NotFound
                    : HttpStatusCode.InternalServerError;

                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to toggle availability"]
                }, statusCode: (int)statusCode);
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new { data = result.Data }
            });
        }



    }
}
