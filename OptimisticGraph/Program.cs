using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int operationsPerThread = 100000;

        private const int numOfThreads = 10;

        private const int totalOps = operationsPerThread*numOfThreads;

        private static readonly Dictionary<GraphOperation, double> opDistribution = new Dictionary<GraphOperation, double>
        {
            {GraphOperation.AddVertex, 0.249},
            {GraphOperation.AddEdge, 0.15},
            {GraphOperation.RemoveVertex, 0.20},
            {GraphOperation.RemoveEdge, 0.15},
            {GraphOperation.ContainsVertex, 0.15},
            {GraphOperation.ContainsEdge, 0.1},
            {GraphOperation.BFS, 0.001},
        };

        public static IGraph graph;

        public static void Main()
        {
            List<Thread> workerThreads;
            int i, j;
            Stopwatch watch;

            IGraph[] graphs =
            {
                new CoarseGrainLockGraph(), 
                new Graph(),
                new ExtendedCoarseGrainLockGraph(),
                new NaiveExtendedGraph(),
                new ExtendedGraph(),
            };

            Console.WriteLine($"Starting {opDistribution.Count} types of operations on {graphs.Length} types of graphs.");
            Console.WriteLine($"{numOfThreads} threads will perform {totalOps/1000}K operations.");

            for (j = 0; j < graphs.Length; j++)
            {
                graph = graphs[j];

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

                Console.WriteLine(graph.GetType());
                Console.WriteLine($"Throughput: {throughput} KOps/sec");
                Console.WriteLine($"Graph contains {graph.GetVertexCount()} vertices.");
            }

            PrintCoreInfo();

            Thread.Sleep(500000);
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
            Console.WriteLine("Extracting Processors Core Information:");
            try
            {
                // Physical Processors:
                foreach (
                    var item in
                    new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
                {
                    Console.WriteLine("Number Of Physical Processors: {0} ", item["NumberOfProcessors"]);
                }

                // Cores:
                int coreCount = 0;
                foreach (
                    var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfCores"].ToString());
                }
                Console.WriteLine("Number Of Cores: {0}", coreCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to retreive Physical Processors and Core count from WMI, {0}", e);
            }
            

            // Logical Processors:
            Console.WriteLine("Number Of Logical Processors: {0}", Environment.ProcessorCount);
        }
    }
}
