using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using backend_dotnet.Data;
using backend_dotnet.Models;
using backend_dotnet.Models.DTOs;
using backend_dotnet.Services;

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

            authGroup.MapPost("/register", Register)
                 .WithName("Register")
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

            var token = jwtTokenGenerator.GenerateToken(user);

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

        private static async Task<IResult> Register(
            RegisterationRequestDto registerationRequestDto,
            ApplicationDbContext db,
            IPasswordHasher passwordHasher)
        {
            if (string.IsNullOrWhiteSpace(registerationRequestDto.Email) ||
                string.IsNullOrWhiteSpace(registerationRequestDto.Password))
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["Email and Password are required"]
                });
            }

            var existingUser = await db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == registerationRequestDto.Email.ToLower());

            if (existingUser is not null)
            {
                return Results.BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessages = ["User with this email already exists"]
                });
            }

            // Parse role sent by frontend (ADMIN, DOCTOR, PATIENT, etc.), default to Role.PATIENT if invalid or empty
            var assignedRole = Enum.TryParse<Role>(registerationRequestDto.Role, true, out var parsedRole)
                ? parsedRole
                : Role.PATIENT;

            // Manual mapping: DTO -> Domain Entity
            var newUser = new User
            {
                Email = registerationRequestDto.Email,
                PasswordHash = passwordHasher.HashPassword(registerationRequestDto.Password),
                Name = registerationRequestDto.Name,
                Role = assignedRole
            };

            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();

            return Results.Created($"/api/auth/{newUser.Id}", new ApiResponse
            {
                IsSuccess = true,
                StatusCode = HttpStatusCode.Created,
                Result = "A new User Registered Successfully"
            });
        }

    }
}