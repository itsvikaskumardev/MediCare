using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.ServiceAppointment;
using backend_dotnet.Services.ServiceAppointment;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_dotnet.Endpoints
{
    public static class ServiceAppointmentEndpoints
    {
        public static void MapServiceAppointmentEndpoints(this IEndpointRouteBuilder app)
        {
            var serviceAppointmentGroup = app.MapGroup("/api/service-appointments").WithTags("ServiceAppointments");

            serviceAppointmentGroup.MapGet("/GetServiceAppointments", GetServiceAppointments)
                .WithName("GetServiceAppointments")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceAppointmentGroup.MapGet("/GetServiceAppointmentById/{id}", GetServiceAppointmentById)
                .WithName("GetServiceAppointmentById")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceAppointmentGroup.MapPost("/CreateServiceAppointment", CreateServiceAppointment)
                .WithName("CreateServiceAppointment")
                .Produces<ApiResponse>(StatusCodes.Status201Created)
                .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse>(StatusCodes.Status409Conflict)
                .Produces<ApiResponse>(StatusCodes.Status502BadGateway)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError).DisableAntiforgery();

            serviceAppointmentGroup.MapPut("/UpdateServiceAppointment/{id}", UpdateServiceAppointment)
                .WithName("UpdateServiceAppointment")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);



            serviceAppointmentGroup.MapPatch("/CancelServiceAppointment/{id}", CancelServiceAppointment)
                .WithName("CancelServiceAppointment")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceAppointmentGroup.MapGet("/GetServiceAppointmentStats", GetServiceAppointmentStats)
                .WithName("GetServiceAppointmentStats")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceAppointmentGroup.MapGet("/GetServiceAppointmentsByPatient", GetServiceAppointmentsByPatient)
                .WithName("GetServiceAppointmentsByPatient")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            serviceAppointmentGroup.MapPost("/VerifyRazorpay", VerifyRazorpay)
                .WithName("VerifyServiceRazorpay")
                .Produces<ApiResponse>(StatusCodes.Status200OK)
                .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        }

        //-------------------------------------------GetServiceAppointments-----------------------------------------------------

        private static async Task<IResult> GetServiceAppointments(
        [AsParameters] GetServiceAppointmentsQueryDTO query,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.GetServiceAppointmentsAsync(query);

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new
                    {
                        appointments = result.Appointments,
                        meta = new
                        {
                            page = result.Page,
                            limit = result.Limit,
                            total = result.Total,
                            count = result.Count
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetServiceAppointments error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------------------GetServiceAppointmentById-----------------------------------------------------
        private static async Task<IResult> GetServiceAppointmentById(
        Guid id,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.GetServiceAppointmentByIdAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Not found"]
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
                Console.Error.WriteLine($"GetServiceAppointmentById error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        //-------------------------------------------CancelServiceAppointment-----------------------------------------------------

        private static async Task<IResult> CreateServiceAppointment(
        [FromBody] CreateServiceAppointmentRequestDTO request,
        HttpContext httpContext,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var authenticatedUserId = httpContext.User?.FindFirst("sub")?.Value
                    ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();

                var result = await serviceAppointmentService.CreateServiceAppointmentAsync(request, authenticatedUserId, origin);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = [result.ErrorMessage ?? "Request failed"]
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Created("/api/service-appointments/CreateServiceAppointment", new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.Created,
                    Result = new
                    {
                        appointment = result.Appointment,
                        checkoutUrl = result.CheckoutUrl
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CreateServiceAppointment unexpected: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        //-------------------------------------------UpdateServiceAppointment-----------------------------------------------------

        private static async Task<IResult> UpdateServiceAppointment(
        Guid id,
        [FromBody] UpdateServiceAppointmentRequestDTO request,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.UpdateServiceAppointmentAsync(id, request);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = [result.ErrorMessage ?? "Request failed"]
                    }, statusCode: (int)result.StatusCode);
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
                Console.Error.WriteLine($"UpdateServiceAppointment error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }


        //-------------------------------------------CancelServiceAppointment-----------------------------------------------------

        private static async Task<IResult> CancelServiceAppointment(
        Guid id,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.CancelServiceAppointmentAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = [result.ErrorMessage ?? "Request failed"]
                    }, statusCode: (int)result.StatusCode);
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
                Console.Error.WriteLine($"CancelServiceAppointment error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        //-------------------------------------------GetServiceAppointmentStats-----------------------------------------------------

        private static async Task<IResult> GetServiceAppointmentStats(IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.GetServiceAppointmentStatsAsync();

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new
                    {
                        services = result.Services,
                        totalServices = result.TotalServices
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetServiceAppointmentStats error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        //-------------------------------------------GetServiceAppointmentsByPatient-----------------------------------------------------

        private static async Task<IResult> GetServiceAppointmentsByPatient(
        [AsParameters] GetServiceAppointmentsByPatientQueryDTO query,
        HttpContext httpContext,
        IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var authenticatedUserId = httpContext.User?.FindFirst("sub")?.Value
                    ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await serviceAppointmentService.GetServiceAppointmentsByPatientAsync(query, authenticatedUserId);

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = result.Data
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetServiceAppointmentsByPatient error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------------------VerifyRazorpay-----------------------------------------------------
        private static async Task<IResult> VerifyRazorpay(
            [FromBody] VerifyServiceRazorpayRequestDTO request,
            IServiceAppointmentService serviceAppointmentService)
        {
            try
            {
                var result = await serviceAppointmentService.VerifyServiceRazorpayPaymentAsync(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = [result.ErrorMessage ?? "Payment verification failed"]
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new { appointment = result.Appointment }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"VerifyRazorpay error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }

    public class VerifyServiceRazorpayRequestDTO
    {
        public string RazorpayOrderId { get; set; } = null!;
        public string RazorpayPaymentId { get; set; } = null!;
        public string RazorpaySignature { get; set; } = null!;
    }
}
