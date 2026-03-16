using BenchmarkDotNet.Running;

// Run all benchmark classes in this assembly.
// Usage:
//   dotnet run -c Release                       → interactive menu
//   dotnet run -c Release -- --filter "*"       → all benchmarks
//   dotnet run -c Release -- --filter "*Serialize*"
//   dotnet run -c Release -- --filter "*Deserialize*"
//   dotnet run -c Release -- --filter "*DeepCopy*"
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
