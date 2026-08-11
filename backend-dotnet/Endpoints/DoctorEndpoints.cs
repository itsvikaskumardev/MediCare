using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services.Doctor;
using backend_dotnet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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



        //-------------------------------------------GetDoctors-----------------------------------------------------


        private static async Task<IResult> GetDoctors(
        [AsParameters] GetDoctorsRequestDTO getDoctorsRequestDTO,
        IDoctorService doctorService)
        {
            var result = await doctorService.GetDoctorsAsync(getDoctorsRequestDTO);

            if (!result.IsSuccess)
            {
                return Results.InternalServerError(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to fetch doctors"]
                });
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
        Guid id,
        IDoctorService doctorService)
        {
            var result = await doctorService.GetDoctorByIdAsync(id);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage]
                    });
                }

                return Results.InternalServerError(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to fetch doctor"]
                });
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
        Guid id,
        [FromForm] UpdateDoctorRequestDTO updateDoctorRequestDTO,
        IFormFile? image,
        ClaimsPrincipal user,
        IDoctorService doctorService,
        ApplicationDbContext dbContext)
        {
            var authenticatedUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = user.IsInRole("ADMIN");

            if (!isAdmin)
            {
                var doctor = await dbContext.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id || d.UserId == id);
                if (doctor == null || string.IsNullOrEmpty(authenticatedUserId) || !string.Equals(doctor.UserId.ToString(), authenticatedUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Forbid();
                }
            }

            var result = await doctorService.UpdateDoctorAsync(id, updateDoctorRequestDTO, image);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage]
                    });
                }

                if (result.ErrorMessage?.Contains("already in use", StringComparison.OrdinalIgnoreCase) == true)
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
                    ErrorMessages = [result.ErrorMessage ?? "Doctor update failed"]
                });
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
            Guid id,
            IDoctorService doctorService)
        {
            var result = await doctorService.DeleteDoctorAsync(id);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage]
                    });
                }

                return Results.InternalServerError(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to delete doctor"]
                });
            }

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new { message = "Doctor removed" }
            });
        }

        //-------------------------------------------ToggleAvailability-----------------------------------------------------
        private static async Task<IResult> ToggleAvailability(
        Guid id,
        ClaimsPrincipal user,
        IDoctorService doctorService)
        {
            var authenticatedDoctorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(authenticatedDoctorId) || authenticatedDoctorId != id.ToString())
            {
                return Results.Forbid();
            }

            var result = await doctorService.ToggleAvailabilityAsync(id);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage]
                    });
                }

                return Results.InternalServerError(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = [result.ErrorMessage ?? "Failed to toggle availability"]
                });
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
