using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Service;
using backend_dotnet.Services.Service;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_dotnet.Endpoints
{
    public static class ServiceEndpoints
    {
        public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
        {
            var serviceGroup = app.MapGroup("/api/services").WithTags("Services");

            serviceGroup.MapPost("/CreateService", CreateService)
                .WithName("CreateService")
                .Produces<ApiResponse>(StatusCodes.Status201Created)
                .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);


        }


        private static async Task<IResult> CreateService(
        [FromForm] CreateServiceRequestDTO createServiceRequestDTO,
        IFormFile? image,
        IServiceModuleService serviceService)
        {
            try
            {
                var result = await serviceService.CreateServiceAsync(createServiceRequestDTO, image);

                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessages = [result.ErrorMessage ?? "Service creation failed"]
                    });
                }

                return Results.Created("/api/services/CreateService", new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CreateService error: {ex.Message}");
                return Results.InternalServerError(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessages = ["An unexpected error occurred while creating service."]
                });
            }
        }
    }
}
