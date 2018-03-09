using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;

namespace JitBench
{
    [EventSource(Name = "Microsoft-JitBench-Scenario")]
    sealed class JitBenchEventSource : EventSource
    {
        public void Startup() { WriteEvent(1); }
        public void FirstRequest() { WriteEvent(2); }
        public void BeginRequests(int iteration, int count) { WriteEvent(3, iteration, count); }
        public void EndRequests(int iteration, int count) { WriteEvent(4, iteration, count); }

        public static JitBenchEventSource Log = new JitBenchEventSource();
    }

    public class JitBenchHelper
    {
        readonly Stopwatch totalTime;
        readonly bool highRes;
        long serverStartupTime;

        private JitBenchHelper(Stopwatch stopwatch)
        {
            totalTime = stopwatch;
            highRes = Stopwatch.IsHighResolution;
        }

        public static JitBenchHelper Start() => new JitBenchHelper(Stopwatch.StartNew());

        public void LogStartup()
        {
            totalTime.Stop();
            JitBenchEventSource.Log.Startup();
            serverStartupTime = totalTime.ElapsedMilliseconds;
            Console.WriteLine("Server started in {0}ms", serverStartupTime);
            Console.WriteLine();
        }

        public void MakeRequests(string url)
        {
            using (var client = new HttpClient())
            {
                Console.WriteLine($"Starting request to {url}");
                var requestTime = Stopwatch.StartNew();
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode(); // Crash immediately if something is broken
                requestTime.Stop();
                JitBenchEventSource.Log.FirstRequest();
                var firstRequestTime = requestTime.ElapsedMilliseconds;

                Console.WriteLine("Response: {0}", response.StatusCode);
                Console.WriteLine("Request took {0}ms", firstRequestTime);
                Console.WriteLine();
                Console.WriteLine("Cold start time (server start + first request time): {0}ms", serverStartupTime + firstRequestTime);
                Console.WriteLine();
                Console.WriteLine();

                int outerN = 5;

                for (int outer = 0; outer < outerN; outer++)
                {
                    var minRequestTime = long.MaxValue;
                    var maxRequestTime = long.MinValue;
                    int N = 1001;
                    long[] responseTimes = new long[N];

                    Console.WriteLine($"Batch {outer}: running {N} requests");
                    JitBenchEventSource.Log.BeginRequests(outer, N);
                    for (int inner = 0; inner < N; inner++)
                    {
                        requestTime.Restart();
                        response = client.GetAsync(url).Result;
                        requestTime.Stop();

                        long interval = highRes ? requestTime.ElapsedTicks : requestTime.ElapsedMilliseconds;
                        responseTimes[inner] = interval;

                        if (interval < minRequestTime)
                        {
                            minRequestTime = interval;
                        }
                        if (interval > maxRequestTime)
                        {
                            maxRequestTime = interval;
                        }
                    }
                    JitBenchEventSource.Log.EndRequests(outer, N);

                    if (highRes)
                    {
                        double averageResponse = 1000 * ((double)responseTimes.Sum() / N / Stopwatch.Frequency);
                        double medianResponse = 1000 * ((double)responseTimes.OrderBy(t => t).ElementAt(N / 2) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate min response time: {0:F2}ms", (1000 * minRequestTime) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate max response time: {0:F2}ms", (1000 * maxRequestTime) / Stopwatch.Frequency);
                        Console.WriteLine("Steadystate average response time: {0:F2}ms", averageResponse);
                        Console.WriteLine("Steadystate median response time: {0:F2}ms", medianResponse);
                    }
                    else
                    {
                        long averageResponse = responseTimes.Sum() / N;
                        long medianResponse = responseTimes.OrderBy(t => t).ElementAt(N / 2);
                        Console.WriteLine("Steadystate min response time: {0}ms", minRequestTime);
                        Console.WriteLine("Steadystate max response time: {0}ms", maxRequestTime);
                        Console.WriteLine("Steadystate average response time: {0}ms", (int)averageResponse);
                        Console.WriteLine("Steadystate median response time: {0}ms", (int)medianResponse);
                    }
                }
            }
        }

        public void VerifyLibraryLocation()
        {
            var hosting = typeof(WebHostBuilder).GetTypeInfo().Assembly.Location;
            var musicStore = typeof(JitBenchHelper).GetTypeInfo().Assembly.Location;

            Console.WriteLine();
            if (Path.GetDirectoryName(hosting) == Path.GetDirectoryName(musicStore))
            {
                Console.WriteLine("ASP.NET loaded from bin. This is a bug if you wanted crossgen");
                Console.WriteLine("ASP.NET loaded from bin. This is a bug if you wanted crossgen");
                Console.WriteLine("ASP.NET loaded from bin. This is a bug if you wanted crossgen");
            }
            else
            {
                Console.WriteLine("ASP.NET loaded from store");
            }
        }
    }
}
