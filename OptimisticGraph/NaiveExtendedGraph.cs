using System.Collections.Generic;

namespace OptimisticGraph
{
    public class NaiveExtendedGraph: Graph, IExtendedGraph
    {
        public Dictionary<int, int> BFS(int source)
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

            HashSet<object> lockedItems = new HashSet<object> { startVertex };

            q.Enqueue(startVertex);

            while (q.Count > 0)
            {
                Vertex v = q.Dequeue();
                Lock(v);
                lockedItems.Add(v);
                if (v.marked) // validate
                {
                    continue;
                }

                Edge e = v.outgoingHead.next;

                while (e.next != null)
                {
                    Lock(e);
                    lockedItems.Add(e);
                    if (e.marked) // validate
                    {
                        e = e.next;
                        continue;
                    }

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

            foreach (var item in lockedItems)
            {
                Unlock(item);
            }

            return d;
        }
    }
}