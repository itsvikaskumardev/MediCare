namespace backend_dotnet.Endpoints
{
    public static class ServiceAppointmentEndpoints
    {
        public static void MapServiceAppointmentEndpoints(this IEndpointRouteBuilder app)
        {
            var serviceAppointmentGroup = app.MapGroup("/api/service-appointments").WithTags("ServiceAppointments");

            // No API logic yet
        }
    }
}
