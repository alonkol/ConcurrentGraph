using System.Threading;

namespace OptimisticGraph
{
    public class Vertex
    {
        public Vertex(int key)
        {
            this.key = key;
            this.marked = false;
            this.outgoingHead = new Edge(int.MinValue);
            this.outgoingHead.next = new Edge(int.MaxValue);
        }

        public Vertex(int key, int version) : this(key)
        {
            this.version = version;
        }

        public int key;
        public bool marked;
        public Vertex next;
        public Edge outgoingHead;
        public int version;
        public int markedVersion;
    }

    public class VertexPair
    {
        public VertexPair(Vertex v1, Vertex v2)
        {
            this.V1 = v1;
            this.V2 = v2;
        }

        public Vertex V1 { get; private set; }
        public Vertex V2 { get; private set; }
    }

    public class Edge
    {
        public Edge(int key)
        {
            this.key = key;
            this.marked = false;
        }

        public Edge(int key, int version) : this(key)
        {
            this.version = version;
        }

        public int key; // the same key of the vertex the edge points to
        public bool marked;
        public Edge next;
        public int version;
        public int markedVersion;
    }

    class EdgePair
    {
        public EdgePair(Edge e1, Edge e2)
        {
            this.E1 = e1;
            this.E2 = e2;
        }

        public Edge E1 { get; private set; }
        public Edge E2 { get; private set; }
    }

    public class Graph: IGraph
    {
        public Graph()
        {
            _verticesHead = new Vertex(int.MinValue);
            _verticesHead.next = new Vertex(int.MaxValue);
        }

        private readonly Vertex _verticesHead;

        public VertexPair LocateVertex(int key)
        {
            while (true)
            {
                Vertex v1 = GetVertexParentOrInsertionPoint(key);
                Vertex v2 = v1.next;

                Lock(v1, v2);

                if (ValidateVertex(v1, v2))
                {
                    return new VertexPair(v1, v2);
                }

                Unlock(v1, v2);
            }
        }

        public void AddVertex(int key)
        {
            VertexPair pair = LocateVertex(key); // v1,v2 still locked at this point
            Vertex v1 = pair.V1;
            Vertex v2 = pair.V2;
            if (v2.key != key) // if does not exist
            {
                Vertex v3 = new Vertex(key);
                v3.next = v2;
                v1.next = v3;
            }
            Unlock(v1, v2);
        }

        public void RemoveVertex(int key)
        {
            VertexPair pair = LocateVertex(key); // v1,v2 still locked at this point
            Vertex v1 = pair.V1;
            Vertex v2 = pair.V2;
            if (v2.key == key)
            {
                v2.marked = true; // logically remove
                v1.next = v2.next; // physically remove

                Unlock(v1, v2);

                RemoveIncomingEdge(key);
            }
            else
            {
                Unlock(v1, v2);
            }
        }

        public void RemoveIncomingEdge(int key)
        {
            Vertex tmp = _verticesHead;
            while (tmp.next != null)
            {
                RemoveIncomingEdge(tmp ,key);
                tmp = tmp.next;
            }
        }

        private void RemoveIncomingEdge(Vertex v, int key)
        {
            while (true)
            {
                Edge e1 = v.outgoingHead;
                Edge e2 = e1.next;
                while (e2.key < key)
                {
                    e1 = e2;
                    e2 = e2.next;
                }

                if (e2.key != key) // key isn't present
                {
                    return;
                }

                lock (e1)
                    lock (e2)
                    {
                        if (ValidateEdge(e1, e2))
                        {
                            e2.marked = true; // logically remove
                            e1.next = e2.next; // physically remove
                            return;
                        }
                    }
            }
        }

        public void AddEdge(int u, int v)
        {
            EdgePair pair = LocateEdge(u, v);
            if (pair != null)
            {
                Edge e1 = pair.E1;
                Edge e2 = pair.E2;

                if (e2.key != v)
                {
                    Edge e3 = new Edge(v);
                    e3.next = e2;
                    e1.next = e3;
                }
                Unlock(e1, e2);
            }
        }

        public void RemoveEdge(int u, int v)
        {
            EdgePair pair = LocateEdge(u, v);

            if (pair == null)
            {
                return;
            }

            Edge e1 = pair.E1;
            Edge e2 = pair.E2;

            if (pair.E2.key == v)
            {
                e2.marked = true; // remove logically
                e1.next = e2.next;  // remove physically
            }

            Unlock(e1, e2);
        }

        public bool ContainsVertex(int key)
        {
            Vertex v = _verticesHead;

            while (v.key < key)
            {
                v = v.next;
            }

            return v.key == key && !v.marked;
        }

        public bool ContainsEdge(int u, int v)
        {
            VertexPair pair = HelpSearchEdge(u, v);

            if (pair == null)
            {
                return false;
            }

            Edge e = pair.V1.outgoingHead;

            while (e.key < v)
            {
                e = e.next;
            }

            return e.key == v && !e.marked;
        }

        private EdgePair LocateEdge(int u, int v)
        {
            VertexPair pair = HelpSearchEdge(u, v);
            if (pair == null)
            {
                return null;
            }
            Vertex v1 = pair.V1;
            Vertex v2 = pair.V2;

            if (v1.marked || v2.marked)
            {
                return null;
            }

            while (true)
            {
                Edge e1 = v1.outgoingHead;
                Edge e2 = e1.next;
                while (e2.key < v)
                {
                    e1 = e2;
                    e2 = e2.next;
                }
                Lock(e1, e2);
                if (ValidateEdge(e1, e2))
                {
                    return new EdgePair(e1,e2);
                }
                Unlock(e1, e2);
            }
        }

        private VertexPair HelpSearchEdge(int u, int v)
        {
            int smallerkey = u < v ? u : v;
            int largerkey = u < v ? v : u;
            Vertex v1 = _verticesHead;
            while (v1.key < smallerkey)
            {
                v1 = v1.next;
            }
            if (v1.key != smallerkey || v1.marked)
            {
                return null;
            }
            Vertex v2 = v1.next;
            while (v2.key < largerkey)
            {
                v2 = v2.next;
            }
            if (v2.key != largerkey || v2.marked)
            {
                return null;
            }
            return smallerkey == u ? new VertexPair(v1, v2) : new VertexPair(v2, v1);
        }

        private bool ValidateVertex(Vertex v1, Vertex v2)
        {
            return !v1.marked && !v2.marked && v1.next == v2;
        }

        private bool ValidateEdge(Edge e1, Edge e2)
        {
            return !e1.marked && !e2.marked && e1.next == e2;
        }

        protected static void Unlock(object n1, object n2 = null)
        {
            Monitor.Exit(n1);
            if (n2 != null)
            {
                Monitor.Exit(n2);
            }
        }

        protected static void Lock(object n1, object n2 = null)
        {
            Monitor.Enter(n1);
            if (n2 != null)
            {
                Monitor.Enter(n2);
            }
        }
        protected Vertex GetVertexParentOrInsertionPoint(int key)
        {
            Vertex v1 = _verticesHead;
            Vertex v2 = v1.next;

            while (v2.key < key)
            {
                v1 = v2;
                v2 = v2.next;
            }

            return v1;
        }

        protected Vertex GetVertex(int key)
        {
            Vertex parent = this.GetVertexParentOrInsertionPoint(key);
            return parent.next.key == key ? parent.next : null;
        }

        public int GetVertexCount()
        {
            int cnt = -2; // _verticesHead contains two dummy nodes
            Vertex v = _verticesHead;
            while (v != null)
            {
                v = v.next;
                cnt++;
            }
            return cnt;
        }

    }

}