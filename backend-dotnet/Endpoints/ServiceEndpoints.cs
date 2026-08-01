namespace backend_dotnet.Endpoints
{
    public static class ServiceEndpoints
    {
        public static void MapServiceEndpoints(this IEndpointRouteBuilder app)
        {
            var serviceGroup = app.MapGroup("/api/services").WithTags("Services");

            // No API logic yet
        }
    }
}
