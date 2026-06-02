using System.Numerics;
using System.Reflection;
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7077/cupid")
    .Build();

connection.On<string, string, int, string?, string>(
    "ReceiveLetter",
    async (name, city, age, phone, message) =>
    {
        Console.WriteLine();
        Console.WriteLine("==================================");
        Console.WriteLine($"Pismo od: {name}");
        Console.WriteLine($"Grad: {city}");
        Console.WriteLine($"Godine: {age}");

        Console.WriteLine(message);

        if (!string.IsNullOrEmpty(phone))
        {
            Console.WriteLine($"Telefon: {phone}");
        }

        Console.WriteLine("==================================");
        Console.WriteLine();

        Console.WriteLine("Potvrdite prijem pisma pritiskom na ENTER...");
        Console.ReadLine();

        await connection.InvokeAsync("Confirm");

        Console.WriteLine("Prijem pisma potvrđen.");
    });

bool registered = false;
bool lastAttemptFailed = false;

connection.On<string>("Error", msg =>
{
    Console.WriteLine($"Greška: {msg}");
    lastAttemptFailed = true;
});

connection.On<string>("Info", msg =>
{
    Console.WriteLine(msg);
    registered = true;
});

await connection.StartAsync();

while (!registered)
{
    lastAttemptFailed = false;

    UserInput userInput = ReadUserInput();

    await connection.InvokeAsync(
        "InitSinglePerson",
        userInput.Username,
        userInput.City,
        userInput.Age,
        userInput.Phone,
        userInput.Gender);

    Console.WriteLine("\nČekanje odgovora servera...\n");

    await Task.Delay(300);

    if (registered)
        break;

    if (lastAttemptFailed)
    {
        Console.WriteLine("\nRegistracija neuspešna.");

        Console.WriteLine("Da li želite da pokušate ponovo? (y/n)");
        var retry = Console.ReadLine();

        if (retry?.ToLower() != "y")
            break;
    }
}

Console.WriteLine();
Console.WriteLine("Komande:");
Console.WriteLine("/block username");
Console.WriteLine("/exit");
Console.WriteLine();

while (true)
{
    string command = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(command))
        continue;

    if (command.StartsWith("/block "))
    {
        string blockedUser = command.Substring(7).Trim();

        if (string.IsNullOrWhiteSpace(blockedUser))
        {
            Console.WriteLine("Morate navesti username.");
            continue;
        }

        await connection.InvokeAsync("Block", blockedUser);

        Console.WriteLine($"Korisnik {blockedUser} je blokiran.");
    }
    else if (command.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    else
    {
        Console.WriteLine("Nepoznata komanda.");
    }
}

await connection.StopAsync();

UserInput ReadUserInput()
{
    string username;
    string city;
    int age;
    string phone;
    bool gender;

    do
    {
        Console.WriteLine("Korisničko ime:");
        username = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
            Console.WriteLine("Korisničko ime ne sme biti prazno!");
    }
    while (string.IsNullOrWhiteSpace(username));

    do
    {
        Console.WriteLine("Grad:");
        city = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(city))
            Console.WriteLine("Grad ne sme biti prazan!");
    }
    while (string.IsNullOrWhiteSpace(city));

    while (true)
    {
        Console.WriteLine("Godine:");

        if (int.TryParse(Console.ReadLine(), out age) && age > 0)
            break;

        Console.WriteLine("Unesite validan pozitivan broj.");
    }

    do
    {
        Console.WriteLine("Telefon:");
        phone = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(phone))
            Console.WriteLine("Telefon ne sme biti prazan!");
    }
    while (string.IsNullOrWhiteSpace(phone));

    while (true)
    {
        Console.WriteLine("Pol (zenski/muski):");

        var input = Console.ReadLine()?.Trim().ToLower();

        if (input == "zenski")
        {
            gender = true;
            break;
        }

        if (input == "muski")
        {
            gender = false;
            break;
        }

        Console.WriteLine("Unesite 'zenski' ili 'muski'.");
    }

    return new UserInput(username, city, age, phone, gender);
}

record UserInput(
    string Username,
    string City,
    int Age,
    string Phone,
    bool Gender);