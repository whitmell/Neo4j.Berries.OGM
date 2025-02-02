using Neo4j.Berries.OGM;
using MovieGraph.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddNeo4j<ApplicationGraphContext>(builder.Configuration, options =>
{
    options
        .ConfigureFromAssemblies(typeof(Program).Assembly);
    options.EnableTimestamps();
    options.EnforceIdentifiers = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();




app.Run();
