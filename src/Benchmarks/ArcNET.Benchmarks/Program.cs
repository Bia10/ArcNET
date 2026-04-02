using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// BenchmarkSwitcher discovers all [Benchmark]-attributed classes in the assembly.
// Invoke: dotnet run -c Release -- --filter "*" --join
