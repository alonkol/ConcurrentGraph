using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace OptimisticGraph
{
    enum GraphOperation
    {
        AddVertex,
        AddEdge,
        RemoveVertex,
        RemoveEdge,
        ContainsVertex,
        ContainsEdge,
        BFS,
    }

    class Program
    {
        private static int operationsPerThread = 10000;
        
        private static int numOfThreads = 4;

        private static int totalOps;

        private static readonly Dictionary<GraphOperation, double> opDistribution = new Dictionary<GraphOperation, double>
        {
            {GraphOperation.AddVertex, 0.2},
            {GraphOperation.AddEdge, 0.2},
            {GraphOperation.RemoveVertex, 0.2},
            {GraphOperation.RemoveEdge, 0.2},
            {GraphOperation.ContainsVertex, 0.05},
            {GraphOperation.ContainsEdge, 0.05},
            {GraphOperation.BFS, 0.1},
        };

        public static IGraph graph;

        private static StreamWriter log;

        public static void Main(string[] argStrings)
        {
            if (argStrings.Length > 2)
            {
                Console.WriteLine("Usage: ExtendedGraph.exe [numOfThreads [operationsPerThread]]");
                return;
            }
            if (argStrings.Length > 0)
            {
                numOfThreads = int.Parse(argStrings[0]);
            }
            if (argStrings.Length == 2)
            {
                operationsPerThread = int.Parse(argStrings[1]);
            }

            totalOps = operationsPerThread * numOfThreads;

            List<Thread> workerThreads;
            int i, j;
            Stopwatch watch;

            IGraph[] graphs =
            {
                new CoarseGrainLockGraph(), 
                new Graph(),
                new ExtendedCoarseGrainLockGraph(),
                new ExtendedGraph(),
            };

            try
            {
                using (log = File.AppendText($"log_{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt"))
                {
                    Log("####################################################################");
                    Log($"Starting, {DateTime.UtcNow}");
                    Log($"Performing {opDistribution.Count} types of operations on {graphs.Length} types of graphs.");
                    Log($"{numOfThreads} threads will perform {totalOps/1000}K operations.");
                    Log("####################################################################");

                    for (j = 0; j < graphs.Length; j++)
                    {
                        graph = graphs[j];
                        Console.WriteLine(graph.GetType());

                        workerThreads = new List<Thread>();
                        watch = Stopwatch.StartNew();

                        for (i = 0; i < numOfThreads; i++)
                        {
                            Thread thread = new Thread(ThreadWorker);
                            workerThreads.Add(thread);
                            thread.Start();
                        }

                        foreach (Thread thread in workerThreads)
                        {
                            thread.Join();
                        }

                        watch.Stop();

                        int throughput = (int) (totalOps/(watch.Elapsed.Milliseconds/1000.0))/1000;

                        Log($"----{graph.GetType()}----");
                        Log($"Throughput: {throughput} KOps/sec");
                        Log($"Graph contains {graph.GetVertexCount()} vertices.");
                    }

                    PrintCoreInfo();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed: {e}");
            }
            
        }

        public static void ThreadWorker()
        {
            int total = operationsPerThread;

            GraphOperation op;
            int u, v;
            Random rand = new Random();
            int vertexId = 1;

            while (total > 0)
            {
                op = GetRandomOperation(rand);
                u = rand.Next() % vertexId;
                v = rand.Next() % vertexId;

                switch (op)
                {
                    case GraphOperation.AddVertex:
                        u = vertexId;
                        vertexId++;
                        graph.AddVertex(u);
                        break;
                    case GraphOperation.AddEdge:
                        graph.AddEdge(u, v);
                        break;
                    case GraphOperation.RemoveVertex:
                        graph.RemoveVertex(u);
                        break;
                    case GraphOperation.RemoveEdge:
                        graph.RemoveEdge(u, v);
                        break;
                    case GraphOperation.ContainsVertex:
                        graph.ContainsVertex(u);
                        break;
                    case GraphOperation.ContainsEdge:
                        break;
                    case GraphOperation.BFS:
                        IExtendedGraph extendedGraph = graph as IExtendedGraph;
                        extendedGraph?.BFS(u);
                        break;
                }

                total--;
            }
        }

        private static GraphOperation GetRandomOperation(Random rand)
        {
            int num = rand.Next(1000);
            int total = 0;
            foreach (KeyValuePair<GraphOperation, double> opPair in opDistribution)
            {
                total += (int) (opPair.Value*1000);
                if (total > num)
                {
                    return opPair.Key;
                }
            }

            return (GraphOperation) rand.Next(opDistribution.Count);
        }

        private static void ThreadPrint(string msg)
        {
            // int id = Thread.CurrentThread.ManagedThreadId;
            // Console.WriteLine($"{id}: {msg}");
        }

        private static void PrintCoreInfo()
        {
            Log("Extracting processors information:");
            try
            {
                // Physical Processors:
                foreach (
                    var item in
                    new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
                {
                    Log($"Number of physical processors: {item["NumberOfProcessors"]}");
                }

                // Cores:
                int coreCount = 0;
                foreach (
                    var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfCores"].ToString());
                }
                Log($"Number of cores: {coreCount}");
            }
            catch (Exception e)
            {
                Log($"Failed to retreive data from WMI, {e}");
            }
            
            // Logical Processors:
            Log($"Number of logical processors: {Environment.ProcessorCount}");
        }

        private static void Log(object o)
        {
            if (log == null) return;

            try
            {
                log.WriteLine(o);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
