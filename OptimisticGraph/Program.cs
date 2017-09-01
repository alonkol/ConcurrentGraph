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
            {GraphOperation.AddVertex, 0.249},
            {GraphOperation.AddEdge, 0.25},
            {GraphOperation.RemoveVertex, 0.15},
            {GraphOperation.RemoveEdge, 0.15},
            {GraphOperation.ContainsVertex, 0.1},
            {GraphOperation.ContainsEdge, 0.1},
            {GraphOperation.BFS, 0.001},
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
                using (log = File.AppendText(string.Format("log_{0:yyyyMMdd-HHmmss}.txt", DateTime.UtcNow)))
                {
                    Log("####################################################################");
                    Log(string.Format("Starting, {0}", DateTime.UtcNow));
                    Log(string.Format("Performing {0} types of operations on {1} types of graphs.", opDistribution.Count, graphs.Length));
                    Log(string.Format("{0} threads will perform {1}K operations.", numOfThreads, totalOps / 1000));
                    Log(string.Format("BFS Ratio: {0}", opDistribution[GraphOperation.BFS]));
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

                        int throughput = (int) (totalOps/(watch.Elapsed.TotalMilliseconds/1000.0))/1000;

                        Log(string.Format("----{0}----", graph.GetType()));
                        Log(string.Format("Throughput: {0} KOps/sec", throughput));
                        Log(string.Format("Graph contains {0} vertices.", graph.GetVertexCount()));
                    }

                    PrintCoreInfo();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed: {0}", e);
                Thread.Sleep(TimeSpan.FromMinutes(1));
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
                        if (extendedGraph != null)
                        {
                            extendedGraph.BFS(u);
                        }
                        
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
                    Log(string.Format("Number of physical processors: {0}", item["NumberOfProcessors"]));
                }

                // Cores:
                int coreCount = 0;
                foreach (
                    var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfCores"].ToString());
                }
                Log(string.Format("Number of cores: {0}", coreCount));
            }
            catch (Exception)
            {
                // ignored
            }

            // Logical Processors:
            Log(string.Format("Number of logical processors: {0}", Environment.ProcessorCount));
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
