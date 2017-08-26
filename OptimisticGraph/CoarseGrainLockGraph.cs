namespace OptimisticGraph
{
    public class CoarseGrainLockGraph: IGraph
    {
        public CoarseGrainLockGraph()
        {
            _verticesHead = new Vertex(int.MinValue);
            _verticesHead.next = new Vertex(int.MaxValue);
        }

        protected readonly Vertex _verticesHead;
        protected readonly object _lockObject = new object();

        public void AddVertex(int key)
        {
            lock (_lockObject)
            {
                Vertex v1 = GetVertexParentOrInsertionPoint(key);
                Vertex v2 = v1.next;

                if (v2.key == key)
                {
                    return;
                }

                Vertex v3 = new Vertex(key);
                v3.next = v2;
                v1.next = v3;

            }
        }

        public void AddEdge(int u, int v)
        {
            lock (_lockObject)
            {
                Vertex u1 = GetVertex(u);

                if (u1 != null)
                {
                    Edge e1 = u1.outgoingHead;
                    Edge e2 = e1.next;

                    while (e2.key < v)
                    {
                        e1 = e2;
                        e2 = e2.next;
                    }

                    if (e2.key != v)
                    {
                        Edge e3 = new Edge(v);
                        e3.next = e2;
                        e1.next = e3;
                    }
                }
            }
        }

        public void RemoveVertex(int key)
        {
            lock (_lockObject)
            {
                Vertex v1 = GetVertexParentOrInsertionPoint(key);
                Vertex v2 = v1.next;

                if (v2.key == key)
                {
                    v1.next = v2.next;
                    RemoveIncomingEdges(key);
                }
            }
        }

        public void RemoveEdge(int u, int v)
        {
            lock (_lockObject)
            {
                Vertex uVertex = GetVertex(u);

                if (uVertex != null)
                {
                    Vertex vVertex = GetVertex(v);

                    if (vVertex != null)
                    {
                        Edge e1 = uVertex.outgoingHead;
                        Edge e2 = e1.next;

                        while (e2.key < v)
                        {
                            e1 = e2;
                            e2 = e2.next;
                        }

                        if (e2.key == v)
                        {
                            e1.next = e2.next;
                        }
                    }
                }
            }
        }

        public bool ContainsVertex(int key)
        {
            lock (_lockObject)
            {
                return GetVertex(key) != null;
            }
        }

        public bool ContainsEdge(int u, int v)
        {
            lock (_lockObject)
            {
                Vertex uVertex = GetVertex(u);

                if (uVertex != null)
                {
                    Edge e2 = uVertex.outgoingHead;

                    while (e2.key < v)
                    {
                        e2 = e2.next;
                    }

                    return e2.key == v;
                }

                return false;
            }
        }

        private void RemoveIncomingEdges(int key)
        {
            Vertex v = _verticesHead;
            while (v.next != null)
            {
                Edge e1 = v.outgoingHead;
                Edge e2 = e1.next;
                while (e2.key < key)
                {
                    e1 = e2;
                    e2 = e1.next;
                }

                if (e2.key == key)
                {
                    e1.next = e2.next;
                }

                v = v.next;
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
            lock (_lockObject)
            {
                int cnt = 0;
                Vertex v = _verticesHead;
                while (v.next != null)
                {
                    v = v.next;
                    cnt++;
                }

                return --cnt;
            }
        }
    }

}
