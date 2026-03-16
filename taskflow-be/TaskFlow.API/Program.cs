using System.Reflection;
using System.Text.Json.Serialization;
using TaskFlow.API.Middleware;
using TaskFlow.Application;
using TaskFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// PHẦN 1: ĐĂNG KÝ SERVICES (DI Container)
// Tất cả services được đăng ký TRƯỚC khi Build()
// ===================================================================

// Đăng ký services từ Application layer (MediatR, AutoMapper, FluentValidation, Behaviors)
builder.Services.AddApplication();

// Đăng ký services từ Infrastructure layer (DbContext, Repositories, UnitOfWork, JWT Auth)
builder.Services.AddInfrastructure(builder.Configuration);

// Đăng ký Controllers (ASP.NET Core MVC)
// JsonStringEnumConverter: serialize enum thành string ("Todo", "InProgress")
// thay vì số (0, 1) → dễ đọc hơn trên Swagger UI và response JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ===== CORS (Cross-Origin Resource Sharing) =====
// Browser có policy bảo mật: chỉ cho phép request cùng origin (cùng domain + port).
// Frontend (localhost:3000) gọi API (localhost:5156) = khác origin → browser BLOCK!
//
// CORS cho phép API chỉ định "origin nào được phép gọi tôi".
// Chỉ cần ở Development. Production thường cùng domain nên không cần.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")  // Chỉ cho phép Frontend
              .AllowAnyHeader()                       // Cho phép gửi bất kỳ header (Authorization, Content-Type...)
              .AllowAnyMethod();                      // Cho phép GET, POST, PUT, DELETE, PATCH...
    });
});

// Swagger - tạo trang test API tự động
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Thông tin hiển thị trên header Swagger UI
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskFlow API",
        Version = "v1",
        Description = "Task Management API - Built with Clean Architecture, CQRS, MediatR",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Le Nhat Huy",
            Email = "your-email@example.com"
        }
    });

    // Đọc XML comments từ file .xml (do <GenerateDocumentationFile> tạo ra)
    // Swagger sẽ hiển thị /// <summary> của mỗi action method trên UI
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // Cấu hình Swagger hỗ trợ JWT
    // Khi test API trên Swagger, có thể nhập Bearer token
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT token (không cần gõ 'Bearer ' ở đầu). Lấy token từ /api/auth/login"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ===================================================================
// PHẦN 2: CẤU HÌNH MIDDLEWARE PIPELINE
// THỨ TỰ RẤT QUAN TRỌNG! Request đi qua từ trên xuống dưới.
// ===================================================================

// 1. Swagger UI (chỉ bật ở Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        // Mở Swagger UI tại root URL (http://localhost:5156/)
        // thay vì phải gõ /swagger
        options.RoutePrefix = string.Empty;
        // Collapse tất cả sections mặc định → UI gọn hơn
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

// 2. Exception Handling Middleware - BỌC TOÀN BỘ pipeline
//    Đặt ở đầu để catch exception từ BẤT KỲ middleware/controller nào phía sau
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 3. CORS - PHẢI đặt trước Authentication!
//    Browser gửi "preflight request" (OPTIONS) trước request thật.
//    CORS middleware phải xử lý OPTIONS trước khi Auth kiểm tra token.
app.UseCors("AllowFrontend");

// 4. HTTPS Redirection - chuyển HTTP → HTTPS
app.UseHttpsRedirection();

// 4. Authentication - ĐỌC JWT token từ header, xác thực user
//    PHẢI trước Authorization! (phải biết "ai" trước khi kiểm tra "có quyền không")
app.UseAuthentication();

// 5. Authorization - kiểm tra user có quyền access endpoint không
//    [Authorize] attribute trên Controller/Action sẽ được check ở đây
app.UseAuthorization();

// 6. Map Controllers - route request đến đúng Controller/Action
app.MapControllers();

app.Run();
