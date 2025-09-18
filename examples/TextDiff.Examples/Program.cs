using TextDiff;
using TextDiff.Exceptions;
using TextDiff.Models;

namespace TextDiff.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("TextDiff.Sharp Examples");
        Console.WriteLine("======================");
        Console.WriteLine();

        var examples = new Dictionary<string, Func<Task>>
        {
            ["1"] = () => BasicDiffExample.Run(),
            ["2"] = () => AsyncProcessingExample.Run(),
            ["3"] = () => MemoryOptimizedExample.Run(),
            ["4"] = () => StreamingExample.Run(),
            ["5"] = () => ErrorHandlingExample.Run(),
            ["6"] = () => ProgressReportingExample.Run(),
            ["7"] = () => CustomDependenciesExample.Run(),
            ["8"] = () => RealWorldScenarioExample.Run(),
            ["9"] = () => PerformanceComparisonExample.Run()
        };

        if (args.Length > 0 && examples.ContainsKey(args[0]))
        {
            await examples[args[0]]();
            return;
        }

        await ShowMenu(examples);
    }

    static async Task ShowMenu(Dictionary<string, Func<Task>> examples)
    {
        while (true)
        {
            Console.WriteLine("Available Examples:");
            Console.WriteLine("1. Basic Diff Processing");
            Console.WriteLine("2. Async Processing with Cancellation");
            Console.WriteLine("3. Memory-Optimized Processing");
            Console.WriteLine("4. Streaming for Large Files");
            Console.WriteLine("5. Error Handling Patterns");
            Console.WriteLine("6. Progress Reporting");
            Console.WriteLine("7. Custom Dependencies");
            Console.WriteLine("8. Real-World Scenario");
            Console.WriteLine("9. Performance Comparison");
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select an example (0-9): ");

            var input = Console.ReadLine();
            if (input == "0") break;

            if (examples.ContainsKey(input))
            {
                Console.WriteLine();
                try
                {
                    await examples[input]();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Example failed: {ex.Message}");
                }
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
            else
            {
                Console.WriteLine("Invalid selection. Please try again.");
            }
        }
    }
}

// Example classes will be defined in separate files