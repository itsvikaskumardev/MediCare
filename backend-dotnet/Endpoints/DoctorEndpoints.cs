using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Doctor;
using backend_dotnet.Services.Doctor;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
        }

        private static async Task<IResult> CreateDoctor(
            [FromForm] CreateDoctorRequestDTO createDoctorRequestDTO,
            IFormFile? image,
            IDoctorService doctorService)
        {
            var result = await doctorService.CreateDoctorAsync(createDoctorRequestDTO, image);

            if (!result.IsSuccess)
            {
                var statusCode = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true
                    ? HttpStatusCode.Conflict
                    : HttpStatusCode.BadRequest;

                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessages = [result.ErrorMessage ?? "Doctor registration failed"]
                }, statusCode: (int)statusCode);
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
    }
}
