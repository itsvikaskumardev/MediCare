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
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError)
                .DisableAntiforgery();/// Disables CSRF validation on that endpoint. It does not change how multipart/form-data is handled.

            serviceGroup.MapGet("/GetServices", GetServices)
                .WithName("GetServices")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceGroup.MapGet("/GetServiceById/{id}", GetServiceById)
                .WithName("GetServiceById")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceGroup.MapPut("/UpdateService/{id}", UpdateService)
                .WithName("UpdateService")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceGroup.MapDelete("/DeleteService/{id}", DeleteService)
                .WithName("DeleteService")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);


        }

        //-----------------------------------CreateService--------------------------------------------

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

        //-----------------------------------GetServices--------------------------------------------
        private static async Task<IResult> GetServices(IServiceModuleService serviceService)
        {
            try
            {
                var result = await serviceService.GetServicesAsync();

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetServices error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-----------------------------------GetServices--------------------------------------------
        private static async Task<IResult> GetServiceById(
        Guid id,
        IServiceModuleService serviceService)
        {
            try
            {
                var result = await serviceService.GetServiceByIdAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Service not found"]
                    });
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetServiceById error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-----------------------------------UpdateService--------------------------------------------

        private static async Task<IResult> UpdateService(
            Guid id,
            [FromForm] UpdateServiceRequestDTO updateServiceRequestDTO,
            IFormFile? image,
            IServiceModuleService serviceService)
        {
            try
            {
                var result = await serviceService.UpdateServiceAsync(id, updateServiceRequestDTO, image);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Service not found"]
                    });
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UpdateService error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-----------------------------------UpdateService--------------------------------------------
        private static async Task<IResult> DeleteService(
        Guid id,
        IServiceModuleService serviceService)
        {
            try
            {
                var result = await serviceService.DeleteServiceAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Service not found"]
                    });
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    ErrorMessages = []
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"DeleteService error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
