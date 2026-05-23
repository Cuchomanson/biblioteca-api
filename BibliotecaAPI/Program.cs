using BibliotecaAPI;
using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Options;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


//Como se añade después del CreateBuilder, este sería el proveedor con mas prioridad
var diccionarioConfiguraciones = new Dictionary<string, string>
{
    {"quien_soy", "Un diccionario en memoria" }
};
builder.Configuration.AddInMemoryCollection(diccionarioConfiguraciones!);



// Área de servicios

builder.Services.AddDataProtection(); //Agrega servicios de protección de datos, que se utilizan para proteger datos sensibles, como cookies de autenticación, tokens antifalsificación, etc. Proporciona una API para cifrar y descifrar datos, lo que ayuda a proteger la información confidencial en la aplicación.


// Caché en memoria
//builder.Services.AddOutputCache(opciones =>
//{
//    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
//});

builder.Services.AddStackExchangeRedisOutputCache(opciones =>
{
    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
});




//Configuración de opciones con validación de datos mediante DataAnnotations
builder.Services.AddOptions<PersonaOpciones>()
    .Bind(builder.Configuration.GetSection(PersonaOpciones.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<TarifaOpciones>()
    .Bind(builder.Configuration.GetSection(TarifaOpciones.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();


builder.Services.AddTransient<ServicioTransient>();
builder.Services.AddScoped<ServicioScoped>();
builder.Services.AddSingleton<ServicioSingleton>();
builder.Services.AddSingleton<PagosProcesamiento>();
builder.Services.AddTransient<IServicioHash, ServicioHash>();
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();

// Filtros que requiren inyección de dependencias
builder.Services.AddScoped<MiFiltroDeAccion>();
builder.Services.AddScoped<FiltroValidacionLibro>();

//Filtro global
builder.Services.AddControllers(opciones =>
{
   opciones.Filters.Add<FiltroTiempoEjecucion>(); //Agrega el filtro de tiempo de ejecución a todas las acciones del controlador
   opciones.Conventions.Add(new ConvencionAgrupaPorVersion());
})
.AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles)//Puesto para que ignore los ciclos al serializar 
.AddNewtonsoftJson();                                                                                                        //con EF. Libro -> Autor -> Libro -> Bucle infinito

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>(); //UserManager es el servicio que se encarga de gestionar los usuarios, como crear, eliminar, actualizar, etc
builder.Services.AddScoped<SignInManager<Usuario>>(); //SignInManager es el servicio que se encarga de gestionar el inicio de sesión, como iniciar sesión, cerrar sesión, etc
builder.Services.AddHttpContextAccessor(); //Permite acceder al contexto HTTP desde cualquier parte de la aplicación, como por ejemplo en los servicios, para obtener información del usuario autenticado, etc

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.MapInboundClaims = false; //Evita que se mapeen automáticamente los claims de JWT a los claims de .NET, lo que permite mantener los nombres originales de los claims en el token JWT.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, //No validamos el emisor del token
        ValidateAudience = false, //No validamos el destinatario del token
        ValidateLifetime = true, //Validamos la expiración del token
        ValidateIssuerSigningKey = true, //Validamos la firma del token, llave privada que se utiliza para firmar el token, debe ser la misma que se utiliza para validar la firma del token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)),
        ClockSkew = TimeSpan.Zero //Elimina el tiempo de gracia para la expiración del token, lo que significa que el token expirará exactamente en el momento especificado en su claim de expiración (exp) sin permitir un margen adicional.
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EsAdmin", policy => policy.RequireClaim("esAdmin"))
    .AddPolicy("EsVendedor", policy => policy.RequireClaim("EsVendedor"));


builder.Services.AddTransient<IServicioUsuarios, ServicioUsuarios>();


builder.Services.AddAutoMapper(typeof(Program));


//CORS
var origenesPermitidos = builder.Configuration.GetSection("origenesPermitidos").Get<string[]>()!;

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins(origenesPermitidos) // Permite cualquier origen (dominio) para las solicitudes CORS
            .AllowAnyMethod() // Permite cualquier método HTTP (GET, POST, PUT, DELETE, etc.) 
            .AllowAnyHeader() // Permite cualquier encabezado en las solicitudes CORS
            .WithExposedHeaders("mi-cabecera", "cantidad-total-registros"); //Permite que desde JS se pueda acceder a datos enviados por la cabecera
    });
});


//Swagger
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Biblioteca API",
        Description = "Web API para trabajar con datos de autores y libros",
        Contact = new OpenApiContact()
        {
            Email = "Cucho@email.com",
            Name = "Cucho",
            Url = new Uri("https://cucho.blog")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/license/mit/")
        }
    });

    opciones.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Biblioteca API",
        Description = "Web API para trabajar con datos de autores y libros",
        Contact = new OpenApiContact()
        {
            Email = "Cucho@email.com",
            Name = "Cucho",
            Url = new Uri("https://cucho.blog")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/license/mit/")
        }
    });

    opciones.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme.",
        In = ParameterLocation.Header
    });

    //** Se puede usar el filtro (solo mostrará candados donde corresponda) o esta parte de abajo que los mostrará en todas partes

    //opciones.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    //{
    //    [new OpenApiSecuritySchemeReference("bearer", document)] = []
    //});

    opciones.OperationFilter<FiltroAutorizacion>();
});





var app = builder.Build();

// Migraciones automáticas al iniciar la aplicación, para evitar tener que ejecutar el comando "Update-Database" cada vez que se hace un cambio en el modelo de datos.
// Esto es útil en entornos de desarrollo, pero en producción es recomendable manejar las migraciones de forma manual para tener un mayor control
// sobre los cambios en la base de datos.
// También es porque por ejemplo en Azure cuando es con seguridad integrada, al desplegar desde VS no se tienen permisos sobre la BD
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

//Área de middlewares
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async contexto =>
{
    var exceptionHandlerFeature = contexto.Features.Get<IExceptionHandlerFeature>();
    var exception = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeDeError = exception.Message,
        StackTrace = exception.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = contexto.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();

    await Results.InternalServerError(new 
    {
        tipo = "Error", 
        mensaje = "Ha ocurrido un error en el servidor", 
        status = 500
    }).ExecuteAsync(contexto);
}));


app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1");
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
});

//Middleware que guardará una traza por cada petición que se recibe
app.UseLogueaPeticion();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/bloqueado")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Acceso denegado");
    }

    await next.Invoke();

});

app.Use(async (contexto, next) =>
{
    contexto.Response.Headers.Append("mi-cabecera", "valor");
    await next();
});

app.UseStaticFiles(); //Habilita el servicio de archivos estáticos, lo que permite servir archivos como imágenes, CSS, JavaScript, etc. desde la carpeta wwwroot de la aplicación.

app.UseCors();

app.UseOutputCache();

app.MapControllers();


app.Run();
