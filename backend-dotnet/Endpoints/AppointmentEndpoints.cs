using backend_dotnet.Models;
using backend_dotnet.Models.DTOs.Appointment;
using backend_dotnet.Services.Appointment;
using System.Net;

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

            // No API logic yet
        }


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
    }
}
