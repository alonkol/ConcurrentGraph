using System.Collections.Generic;

namespace OptimisticGraph
{
    public interface IExtendedGraph: IGraph
    {
        Dictionary<int, int> BFS(int source);
    }
}
