namespace OptimisticGraph
{
    public interface IGraph
    {
        void AddVertex(int key);
        void RemoveVertex(int key);
        void AddEdge(int u, int v);
        void RemoveEdge(int u, int v);
        bool ContainsVertex(int key);
        bool ContainsEdge(int u, int v);
        int GetVertexCount();
    }
}
