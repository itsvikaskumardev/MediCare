using backend_dotnet.Data;

namespace backend_dotnet.Services.Service
{
    public class ServiceModuleService : IServiceModuleService
    {
        private readonly ApplicationDbContext _db;

        public ServiceModuleService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Implementation of Service module methods will be defined here
    }
}
