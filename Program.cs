using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Globalization;
using System.Security.Cryptography;

BenchmarkRunner.Run<GuidBenchmark>();

Console.WriteLine("Verificando ordenamento.");
const int Iterations = 1000; // Número de GUIDs a serem gerados

List<Guid> guids = new(Iterations);
for (int i = 0; i < Iterations; i++)
{
    guids.Add(Guid.NewGuid());
}
int outOfOrderCount = CheckOrder(guids);
Console.WriteLine($"Guid.NewGuid(): {outOfOrderCount} GUIDs fora de ordem.");

List<Guid> guidsV7 = new(Iterations);
for (int i = 0; i < Iterations; i++)
{
    guids.Add(GuidExtensions.GuidV7());
}

outOfOrderCount = CheckOrder(guidsV7);
Console.WriteLine($"GuidExtensions.GuidV7(): {outOfOrderCount} GUIDs fora de ordem.");


List<Guid> guidsV7_net9 = new(Iterations);
for (int i = 0; i < Iterations; i++)
{
    guids.Add(Guid.CreateVersion7());
}

outOfOrderCount = CheckOrder(guidsV7_net9);
Console.WriteLine($"Guid.CreateVersion7(): {outOfOrderCount} GUIDs fora de ordem.");


List<Guid> guidsUlids = new(Iterations);
for (int i = 0; i < Iterations; i++)
{
    guids.Add(Ulid.NewUlid().ToGuid());
}

outOfOrderCount = CheckOrder(guidsUlids);
Console.WriteLine($"Ulid.NewUlid().ToGuid(): {outOfOrderCount} GUIDs fora de ordem.");

/*  Implementations */

int CheckOrder(List<Guid> guids)
{
    int outOfOrderCount = 0;
    // Verifica se os GUIDs estão em ordem crescente
    for (int i = 1; i < guids.Count; i++)
    {
        if (guids[i - 1].CompareTo(guids[i]) > 0)
        {
            outOfOrderCount++;
        }
    }
    return outOfOrderCount;
}

static class GuidExtensions
{
    public static Guid GuidV7()
    {
        Span<byte> uuidAsBytes = stackalloc byte[16];
        var currentDate = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(currentDate);
        }
        currentDate.AsSpan(2..8).CopyTo(uuidAsBytes);

        RandomNumberGenerator.Fill(uuidAsBytes[6..]);

        uuidAsBytes[6] &= 0x0F;
        uuidAsBytes[6] += 0x70;

        return new(uuidAsBytes, true);
    }
}

[MemoryDiagnoser] // Adiciona diagnóstico de memória
[RankColumn] // Classifica os métodos por desempenho
public class GuidBenchmark
{
    [Benchmark]
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }

    [Benchmark]
    public Guid NewGuidV7()
    {
        return GuidExtensions.GuidV7();
    }

    [Benchmark]
    public Guid NewGuidV7_net9()
    {
        return Guid.CreateVersion7();
    }

    [Benchmark]
    public Guid NewUlid()
    {
        return Ulid.NewUlid().ToGuid();
    }
}
public class CustomConfig : ManualConfig
{
    public CustomConfig()
    {
        AddLogger(ConsoleLogger.Default);  // Adiciona o logger padrão para exibir o progresso no console
        AddColumn(StatisticColumn.Mean);  // Exibe a média do tempo gasto
        AddColumn(StatisticColumn.StdDev); // Exibe o desvio padrão
        AddColumn(StatisticColumn.Error);  // Exibe o erro padrão
        AddColumn(RankColumn.Arabic);      // Exibe a classificação
        AddDiagnoser(MemoryDiagnoser.Default);  // Diagnóstico de uso de memória

        // Configuração da cultura para pt-BR
        CultureInfo ptBR = new CultureInfo("pt-BR");
        SummaryStyle style = SummaryStyle.Default.WithCultureInfo(ptBR);
        SummaryStyle = style;

        AddJob(Job.Default
            .WithWarmupCount(3)            // Número de execuções de warmup
            .WithIterationCount(10)        // Número de iterações
            .WithLaunchCount(1));          // Número de lançamentos
    }
}