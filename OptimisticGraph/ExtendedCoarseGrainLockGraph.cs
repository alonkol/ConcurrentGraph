using System.Collections.Generic;

namespace OptimisticGraph
{
    public class ExtendedCoarseGrainLockGraph : CoarseGrainLockGraph, IExtendedGraph
    {
        public Dictionary<int, int> BFS(int source)
        {
            lock (_lockObject)
            {
                Queue<Vertex> q = new Queue<Vertex>();
                Dictionary<int, int> d = new Dictionary<int, int>
                {
                    {source, 0}
                };

                Vertex startVertex = this.GetVertex(source);
                if (startVertex == null)
                {
                    return null;
                }

                q.Enqueue(startVertex);

                while (q.Count > 0)
                {
                    Vertex v = q.Dequeue();
                    Edge e = v.outgoingHead.next;

                    while (e.next != null)
                    {
                        if (!d.ContainsKey(e.key))
                        {
                            d.Add(e.key, d[v.key] + 1);
                            Vertex neighbor = GetVertex(e.key);
                            if (neighbor != null)
                            {
                                q.Enqueue(neighbor);
                            }
                        }
                        e = e.next;
                    }
                }

                return d;
            }
        }
    }
}
