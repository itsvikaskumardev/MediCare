using backend_dotnet.Data;
using backend_dotnet.Models;
using backend_dotnet.Models.Domain;
using backend_dotnet.Models.DTOs;
using backend_dotnet.Services;
using backend_dotnet.Services.Jwt;
using backend_dotnet.Services.Password;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace backend_dotnetWebMinimalExample.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var authGroup = app.MapGroup("/api/auth").WithTags("Authentication");

            authGroup.MapPost("/login", Login)
                 .WithName("Login")
                 .Produces<ApiResponse>(StatusCodes.Status200OK)
                 .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                 .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
                 .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            authGroup.MapPost("/register-patient", RegisterPatient)
                 .WithName("RegisterPatient")
                 .Produces<ApiResponse>(StatusCodes.Status201Created)
                 .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                 .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

            authGroup.MapPost("/register-admin", RegisterAdmin)
                 .WithName("RegisterAdmin")
                 .Produces<ApiResponse>(StatusCodes.Status201Created)
                 .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
                 .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> Login(
            LoginRequestDTO loginRequestDTO,
            ApplicationDbContext db,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            if (string.IsNullOrWhiteSpace(loginRequestDTO.Email) ||
                string.IsNullOrWhiteSpace(loginRequestDTO.Password))
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["UserName and Password are required"]
                });
            }

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginRequestDTO.Email.ToLower());

            if (user is null || !passwordHasher.VerifyPassword(loginRequestDTO.Password, user.PasswordHash ?? ""))
            {
                return Results.Json(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessages = ["Invalid username or password"]
                }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var token = jwtTokenGenerator.GenerateToken(user.Id.ToString(), user.Email, user.Name, user.Role.ToString());

            var userSession = new UserSession
            {
                UserId = user.Id,
                Token = token,
                IsValid = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await db.UserSessions.AddAsync(userSession);
            await db.SaveChangesAsync();

            return Results.Ok(new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.OK,
                Result = new LoginResponseDTO
                {
                    Email = user.Email,
                    Token = token,
                    Role = user.Role.ToString()
                }
            });
        }

        private static async Task<IResult> RegisterPatient(
            backend_dotnet.Models.DTOs.Auth.PatientRegistrationRequestDto requestDto,
            ApplicationDbContext db,
            IPasswordHasher passwordHasher)
        {
            if (string.IsNullOrWhiteSpace(requestDto.Email) ||
                string.IsNullOrWhiteSpace(requestDto.Password))
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["Email and Password are required"]
                });
            }

            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == requestDto.Email.ToLower());

            if (existingUser is not null)
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["User with this email already exists"]
                });
            }

            var newUser = new User
            {
                Email = requestDto.Email,
                PasswordHash = passwordHasher.HashPassword(requestDto.Password),
                Name = requestDto.Name,
                Mobile = requestDto.Mobile,
                Age = requestDto.Age,
                Gender = requestDto.Gender,
                Role = Role.PATIENT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(newUser);

            var newPatient = new Patient
            {
                UserId = newUser.Id,
                BloodGroup = requestDto.BloodGroup,
                MedicalHistory = requestDto.MedicalHistory,
                Allergies = requestDto.Allergies,
                EmergencyContactName = requestDto.EmergencyContactName,
                EmergencyContactNumber = requestDto.EmergencyContactNumber,
                InsuranceProvider = requestDto.InsuranceProvider,
                InsurancePolicyNumber = requestDto.InsurancePolicyNumber
            };

            await db.Patients.AddAsync(newPatient);
            await db.SaveChangesAsync();

            return Results.Created($"/api/auth/{newUser.Id}", new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.Created,
                Result = "A new Patient Registered Successfully"
            });
        }

        private static async Task<IResult> RegisterAdmin(
            backend_dotnet.Models.DTOs.Auth.AdminRegistrationRequestDto requestDto,
            ApplicationDbContext db,
            IPasswordHasher passwordHasher)
        {
            if (string.IsNullOrWhiteSpace(requestDto.Email) ||
                string.IsNullOrWhiteSpace(requestDto.Password))
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["Email and Password are required"]
                });
            }

            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == requestDto.Email.ToLower());

            if (existingUser is not null)
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["User with this email already exists"]
                });
            }

            var newUser = new User
            {
                Email = requestDto.Email,
                PasswordHash = passwordHasher.HashPassword(requestDto.Password),
                Name = requestDto.Name,
                Role = Role.ADMIN,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();

            return Results.Created($"/api/auth/{newUser.Id}", new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.Created,
                Result = "A new Admin Registered Successfully"
            });
        }

    }
}