using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptimisticGraph;

namespace OptimisticGraphTests
{
    [TestClass()]
    public class ExtendedGraphTests
    {
        private IExtendedGraph[] graphs;
        private IExtendedGraph graph;

        private void init()
        {
            graphs[0] = new ExtendedCoarseGrainLockGraph();
            graphs[1] = new NaiveExtendedGraph();
            graphs[2] = new ExtendedGraph();
        }


        [TestMethod]
        public void BFSTest()
        {
            init();

            foreach (IExtendedGraph graph in graphs)
            {
                graph.AddVertex(1);
                graph.AddVertex(2);
                graph.AddVertex(3);
                graph.AddVertex(4);
                graph.AddEdge(1, 2);
                graph.AddEdge(2, 3);
                graph.AddEdge(3, 4);

                Dictionary<int, int> result = graph.BFS(1);

                Assert.AreEqual(0, result[1]);
                Assert.AreEqual(1, result[2]);
                Assert.AreEqual(2, result[3]);
                Assert.AreEqual(3, result[4]);

                result = graph.BFS(3);
                Assert.AreEqual(0, result[3]);
                Assert.AreEqual(1, result[4]);
                Assert.AreEqual(2, result.Count);

                graph.RemoveVertex(3);
                result = graph.BFS(1);
                Assert.AreEqual(0, result[1]);
                Assert.AreEqual(1, result[2]);
                Assert.AreEqual(2, result.Count);
            }
        }

        [TestMethod]
        public void Test1()
        {
            init();

            foreach (var currGraph in graphs)
            {
                this.graph = currGraph;
                Thread[] threads = new Thread[3];

                for (int i = 0; i < 3; i++)
                {
                    threads[i] = new Thread(Test1Thread);
                    threads[i].Start();
                }

                for (int i = 0; i < 3; i++)
                {
                    threads[i].Join();
                }

                Assert.AreEqual(0, graph.GetVertexCount());
            }
        }

        private void Test1Thread()
        {
            addVertices(1000);
            removeVertices(1000);
        }

        private void addVertices(int ops)
        {
            for (int i = 0; i < ops; i++)
            {
                graph.AddVertex(Thread.CurrentContext.ContextID * ops + i);
            }
        }

        private void removeVertices(int ops)
        {
            for (int i = 0; i < ops; i++)
            {
                graph.RemoveVertex(Thread.CurrentContext.ContextID * ops + i);
            }
        }
    }
}