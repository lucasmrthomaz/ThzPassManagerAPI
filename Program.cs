using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Data Source=passwords.db";

builder.Services.AddSingleton(new SqliteConnection(connectionString));

var app = builder.Build();

app.MapGet("/passwords", async (SqliteConnection db) =>
{
    await db.OpenAsync();

    using var cmd = new SqliteCommand("SELECT * FROM passwords", db);
    using var reader = await cmd.ExecuteReaderAsync();

    var passwords = new List<Password>();

    while (await reader.ReadAsync())
    {
        passwords.Add(new Password
        {
            Site = reader.GetString(1),
            Username = reader.GetString(2),
            Passwd = reader.GetString(3)
        });
    }

    return Results.Ok(passwords);
});

app.MapPost("/passwords", async (HttpContext context, SqliteConnection db) =>
{
    var password = await context.Request.ReadFromJsonAsync<Password>();

    await db.OpenAsync();

    using var cmd = new SqliteCommand(
        "INSERT INTO passwords (site, username, password) VALUES (@site, @username, @password)",
        db);

    cmd.Parameters.AddWithValue("@site", password.Site);
    cmd.Parameters.AddWithValue("@username", password.Username);
    cmd.Parameters.AddWithValue("@password", password.Passwd);

    await cmd.ExecuteNonQueryAsync();

    return Results.Created($"/passwords/{password.Site}", password);
});

app.Run();

public class Password
{
    public string Site { get; set; }
    public string Username { get; set; }
    public string? Passwd { get; set; }
}
