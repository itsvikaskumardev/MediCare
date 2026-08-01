namespace backend_dotnet.Endpoints
{
    public static class AppointmentEndpoints
    {
        public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
        {
            var appointmentGroup = app.MapGroup("/api/appointments").WithTags("Appointments");

            // No API logic yet
        }
    }
}
