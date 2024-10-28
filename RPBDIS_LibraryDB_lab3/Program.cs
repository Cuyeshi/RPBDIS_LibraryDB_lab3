using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RPBDIS_LibraryDB_lab3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer("Server=DESKTOP-2GTDQ2V\\SQLEXPRESS;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;"));

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();
app.UseRouting(); // Добавляем для активации системы маршрутизации

app.Use(async (context, next) =>
{
    Console.WriteLine("Setting response encoding to UTF-8.");
    context.Response.ContentType = "text/html; charset=utf-8";
    await next();
});

// Middleware для кэширования данных
app.Use(async (context, next) =>
{
    Console.WriteLine("Entering caching middleware.");
    
    var dbContext = context.RequestServices.GetRequiredService<LibraryDbContext>();
    var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
    var tables = new List<string> { "Genres", "Readers", "Books", "Publishers", "LoanedBooks", "Employees" };
    var cacheDuration = TimeSpan.FromSeconds(282);

    foreach (var table in tables)
    {
        if (!cache.TryGetValue(table, out List<object> cachedData))
        {
            Console.WriteLine($"Caching data for table: {table}");
            cachedData = FetchTableData(table, dbContext);
            cache.Set(table, cachedData.Take(20).ToList(), cacheDuration);
        }
        else
        {
            Console.WriteLine($"Data for table {table} is already cached.");
        }
    }
    await next();
});

List<object> FetchTableData(string tableName, LibraryDbContext dbContext)
{
    Console.WriteLine($"Fetching data from table: {tableName}");
    switch (tableName)
    {
        case "Genres":
            return dbContext.Genres.Take(20).ToList<object>();
        case "Readers":
            return dbContext.Readers.Take(20).ToList<object>();
        case "Books":
            return dbContext.Books.Take(20).ToList<object>();

        case "Publishers":
            return dbContext.Publishers.Take(20).ToList<object>();
        case "LoanedBooks":
            return dbContext.LoanedBooks.Take(20).ToList<object>();
        case "Employees":
            return dbContext.Employees.Take(20).ToList<object>();
        default:
            Console.WriteLine("Table not found in DbContext.");
            return new List<object>();
    }
}

app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/info", async context =>
    {
        Console.WriteLine("Processing /info route.");
        var clientInfo = $"IP: {context.Connection.RemoteIpAddress}, Browser: {context.Request.Headers["User-Agent"]}";
        await context.Response.WriteAsync(clientInfo);
    });

    endpoints.MapGet("/table/{tableName}", async context =>
    {
        var tableName = context.Request.RouteValues["tableName"].ToString();
        Console.WriteLine($"Processing /table/{tableName} route.");

        var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

        if (cache.TryGetValue(tableName, out List<object> cachedData))
        {
            Console.WriteLine($"Found cached data for table: {tableName}");

            
            foreach (var item in cachedData)
            {
                if (item is Book book)
                {
                    await context.Response.WriteAsync($"Title: {book.Title}, Author: {book.Author}, " +
                        $"Published: {book.PublishYear}<br>");
                }
                else if (item is Genre genre)
                {
                    await context.Response.WriteAsync($"Genre: {genre.Name}, Description: {genre.Description}<br>");
                }
                else if (item is Reader reader)
                {
                    await context.Response.WriteAsync($"Reader: {reader.FullName}, BirthDate: {reader.BirthDate}, " +
                        $"Gender: {reader.Gender}, Address: {reader.Address}, Phone: {reader.Phone}, Passport: {reader.Passport} <br>");
                }
                else if (item is Employee employee)
                {
                    await context.Response.WriteAsync($"Employee: {employee.FullName}, Position: {employee.Position}, " +
                        $"HireDate: {employee.HireDate}<br>");
                }
                else if (item is LoanedBook loanedBook)
                {
                    await context.Response.WriteAsync($"LoanId: {loanedBook.LoanId}, BookId: {loanedBook.BookId}, " +
                        $"ReaderId: {loanedBook.ReaderId}, LoanDate: {loanedBook.LoanDate}, ReturnDate: {loanedBook.ReturnDate}," +
                        $"Returned: {loanedBook.Returned}, Employee: {loanedBook.Employee}<br>");
                }
                else if (item is Publisher publisher)
                {
                    await context.Response.WriteAsync($"Title: {publisher.Name}, Author: {publisher.City}, " +
                        $"Published: {publisher.Address}<br>");
                }
            }
        }
        else
        {
            Console.WriteLine($"No cached data found for table: {tableName}");
            await context.Response.WriteAsync("Данные не найдены.");
        }
    });

    endpoints.MapGet("/searchform1", async context =>
    {
        Console.WriteLine("Processing /searchform1 route.");

        if (context.Request.Query.ContainsKey("query"))
        {
            context.Response.Cookies.Append("searchform1_query", context.Request.Query["query"]);
            context.Response.Cookies.Append("searchform1_category", context.Request.Query["category"]);
            await context.Response.WriteAsync("Состояние формы сохранено в Cookies.");
        }
        else
        {
            var query = context.Request.Cookies["searchform1_query"] ?? "";
            var category = context.Request.Cookies["searchform1_category"] ?? "";

            Console.WriteLine("Restoring form state from Cookies.");

            string formHtml = @$"
            <form method='get' action='/searchform1'>
                <label>Поиск:</label><input type='text' name='query' value='{query}' />
                <label>Категория:</label><select name='category'>
                    <option value='fantasy' {(category == "fantasy" ? "selected" : "")}>Фантастика</option>
                    <option value='detective' {(category == "detective" ? "selected" : "")}>Детектив</option>
                    <option value='science' {(category == "science" ? "selected" : "")}>Наука</option>
                    <option value='history' {(category == "history" ? "selected" : "")}>История</option>
                </select>
                <input type='submit' value='Поиск' />
            </form>";
            await context.Response.WriteAsync(formHtml);
        }
    });

    endpoints.MapGet("/searchform2", async context =>
    {
        Console.WriteLine("Processing /searchform2 route.");

        await context.Session.LoadAsync();

        if (context.Request.Query.ContainsKey("query"))
        {
            context.Session.SetString("searchform2_query", context.Request.Query["query"]);
            context.Session.SetString("searchform2_category", context.Request.Query["category"]);
            await context.Response.WriteAsync("Состояние формы сохранено в Session.");
        }
        else
        {
            var query = context.Session.GetString("searchform2_query") ?? "";
            var category = context.Session.GetString("searchform2_category") ?? "";

            Console.WriteLine("Restoring form state from Session.");

            string formHtml = @$"
            <form method='get' action='/searchform1'>
                <label>Поиск:</label><input type='text' name='query' value='{query}' />
                <label>Категория:</label><select name='category'>
                    <option value='fantasy' {(category == "fantasy" ? "selected" : "")}>Фантастика</option>
                    <option value='detective' {(category == "detective" ? "selected" : "")}>Детектив</option>
                    <option value='science' {(category == "science" ? "selected" : "")}>Наука</option>
                    <option value='history' {(category == "history" ? "selected" : "")}>История</option>
                </select>
                <input type='submit' value='Поиск' />
            </form>";
            await context.Response.WriteAsync(formHtml);
        }
    });
});

app.Run(async context =>
{
    Console.WriteLine("Route not found.");
    await context.Response.WriteAsync("Маршрут не найден.");
});

app.Run();
