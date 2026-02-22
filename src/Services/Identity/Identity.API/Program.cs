using System.Text;
using Identity.Application;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Identity.Application.Commands;
using Identity.Application.DTOs;
using Identity.Application.Queries;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WalletFlow Identity API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer", BearerFormat = "JWT", In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ────────────────────────────────────────────────────────────────
var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (RegisterRequest req, IMediator mediator) =>
{
    var cmd = new RegisterCommand(req.Email, req.FirstName, req.LastName, req.Password);
    var result = await mediator.Send(cmd);
    return Results.Created($"/api/users/{result.User.Id}", result);
});

auth.MapPost("/login", async (LoginRequest req, IMediator mediator) =>
{
    var result = await mediator.Send(new LoginCommand(req.Email, req.Password));
    return Results.Ok(result);
});

auth.MapPost("/refresh", async (RefreshTokenRequest req, IMediator mediator) =>
{
    var result = await mediator.Send(new RefreshTokenCommand(req.AccessToken, req.RefreshToken));
    return Results.Ok(result);
});

auth.MapPost("/logout", async (HttpContext ctx, IMediator mediator) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst("sub")?.Value ?? throw new Exception("Unauthorized"));
    var token = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    await mediator.Send(new LogoutCommand(userId, token));
    return Results.NoContent();
}).RequireAuthorization();

var users = app.MapGroup("/api/users").RequireAuthorization();

users.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var user = await mediator.Send(new GetUserByIdQuery(id));
    return user != null ? Results.Ok(user) : Results.NotFound();
});

app.Run();
