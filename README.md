# ConcurrentPinger

ConcurrentPinger is a .NET 9 console application that pings multiple URLs concurrently and reports their response times and statuses. It demonstrates modern C# features, dependency injection, and structured logging.

## Features

- Concurrently pings a list of URLs with configurable concurrency.
- Reports response time, HTTP status, and errors for each URL.
- Uses dependency injection and logging best practices.
- Simple, extensible architecture.

## Requirements

- .NET 9 SDK

## Getting Started

1. **Clone the repository:**

	git clone https://github.com/akaf47/ConcurrentPinger.git 

2. **Build the project:**
						
	cd ConcurrentPinger  
	dotnet build   

3. **Run the application:**

	dotnet run --project .\ConcurrentPing.App

4. **Run the tests:**  
	dotnet test

## Configuration

To customize which URLs are pinged, edit the `urls` array in `ConcurrentPinger.App/Program.cs`:

You can also adjust the maximum concurrency by passing a second argument to `PingManyAsync`:

   var results = await pinger.PingManyAsync(urls, maxConcurrency: 10);

## Logging

The application uses structured console logging with timestamps. You can adjust logging settings in `Program.cs`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.