using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Appointment;
using backend_dotnet.Services.Appointment;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_dotnet.Endpoints
{
    public static class AppointmentEndpoints
    {
        public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
        {
            var appointmentGroup = app.MapGroup("/api/appointments").WithTags("Appointments");

            appointmentGroup.MapGet("/GetAppointments", GetAppointments)
                    .WithName("GetAppointments")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapGet("/GetAppointmentById/{id}", GetAppointmentById)
                    .WithName("GetAppointmentById")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapGet("/GetAppointmentsByPatient", GetAppointmentsByPatient)
                    .RequireAuthorization()
                    .WithName("GetAppointmentsByPatient")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapPost("/CreateAppointment", CreateAppointment)
                    .WithName("CreateAppointment")
                    .Produces<ApiResponse>(StatusCodes.Status201Created)
                    .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                    .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                    .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                    .Produces<ApiResponse>(StatusCodes.Status409Conflict)
                    .Produces<ApiResponse>(StatusCodes.Status502BadGateway)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapPut("/UpdateAppointment/{id}", UpdateAppointment)
                    .WithName("UpdateAppointment")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                    .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapPatch("/CancelAppointment/{id}", CancelAppointment)
                    .WithName("CancelAppointment")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status404NotFound)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapGet("/GetStats", GetStats)
                    .WithName("GetStats")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            appointmentGroup.MapGet("/GetAppointmentsByDoctor/{doctorId}", GetAppointmentsByDoctor)
                    .WithName("GetAppointmentsByDoctor")
                    .Produces<ApiResponse>(StatusCodes.Status200OK)
                    .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                    .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);



        }


        //-------------------------------GetAppointments------------------------------------------------------

        private static async Task<IResult> GetAppointments(
        [AsParameters] GetAppointmentsQueryDTO query,
        IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.GetAppointmentsAsync(query);

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
                Console.Error.WriteLine($"GetAppointments error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetAppointmentById------------------------------------------------------


        private static async Task<IResult> GetAppointmentById(
        Guid id,
        IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.GetAppointmentByIdAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Appointment not found"]
                    });
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
                Console.Error.WriteLine($"GetAppointmentById error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetAppointmentsByPatient------------------------------------------------------
        private static async Task<IResult> GetAppointmentsByPatient(
        [AsParameters] GetAppointmentsByPatientQueryDTO query,
        ClaimsPrincipal user,
        IAppointmentService appointmentService)
        {
            try
            {
                var authenticatedUserId = user.FindFirst("sub")?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var result = await appointmentService.GetAppointmentsByPatientAsync(query, authenticatedUserId);

                if (!result.IsSuccess)
                {
                    if (result.IsAuthError)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.BadRequest(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessages = [result.ErrorMessage ?? "Request failed"]
                    });
                }

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new { appointments = result.Appointments }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetAppointmentsByPatient: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------CreateAppointment------------------------------------------------------
        private static async Task<IResult> CreateAppointment(
        [FromBody] CreateAppointmentRequestDTO request,
        HttpContext httpContext,
        IAppointmentService appointmentService)
        {
            try
            {
                var authenticatedUserId = httpContext.User?.FindFirst("sub")?.Value
                    ?? httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var origin = httpContext.Request.Headers["Origin"].FirstOrDefault();

                var result = await appointmentService.CreateAppointmentAsync(request, authenticatedUserId, origin);

                if (!result.IsSuccess)
                {
                    return Results.Json(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = result.StatusCode,
                        ErrorMessages = [result.ErrorMessage ?? "Request failed"]
                    }, statusCode: (int)result.StatusCode);
                }

                return Results.Created("/api/appointments/CreateAppointment", new ApiResponse
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
                Console.Error.WriteLine($"CreateAppointment unexpected: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
        //-------------------------------UpdateAppointment------------------------------------------------------
        private static async Task<IResult> UpdateAppointment(
        Guid id,
        [FromBody] UpdateAppointmentRequestDTO request,
        IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.UpdateAppointmentAsync(id, request);

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
                    Result = new { appointment = result.Appointment }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UpdateAppointment error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------CancelAppointment------------------------------------------------------

        private static async Task<IResult> CancelAppointment(
            Guid id,
            IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.CancelAppointmentAsync(id);

                if (!result.IsSuccess)
                {
                    return Results.NotFound(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessages = [result.ErrorMessage ?? "Appointment not found"]
                    });
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
                Console.Error.WriteLine($"CancelAppointment error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetStats------------------------------------------------------

        private static async Task<IResult> GetStats(IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.GetStatsAsync();

                return Results.Ok(new ApiResponse
                {
                    IsSuccess = true,
                    StatusCode = HttpStatusCode.OK,
                    Result = new
                    {
                        stats = new
                        {
                            total = result.Total,
                            revenue = result.Revenue,
                            recentLast7Days = result.RecentLast7Days
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetStats error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetAppointmentsByDoctor------------------------------------------------------

        private static async Task<IResult> GetAppointmentsByDoctor(
        string doctorId,
        [AsParameters] GetAppointmentsByDoctorQueryDTO query,
        IAppointmentService appointmentService)
        {
            try
            {
                var result = await appointmentService.GetAppointmentsByDoctorAsync(doctorId, query);

                if (!result.IsSuccess)
                {
                    return Results.BadRequest(new ApiResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorMessages = [result.ErrorMessage ?? "doctorId required"]
                    });
                }

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
                Console.Error.WriteLine($"GetAppointmentsByDoctor error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        //-------------------------------GetAppointmentsByPatient------------------------------------------------------


        //-------------------------------GetAppointmentsByPatient------------------------------------------------------





    }
}
