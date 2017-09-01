using System.Collections.Generic;
using System.Threading;

namespace OptimisticGraph
{
    public class ExtendedGraphV2 : ExtendedGraph
    {

        private int bfsCnt = 0;

        public new Dictionary<int, int> BFS(int source)
        {
            Interlocked.Increment(ref bfsCnt);
            int ver = Interlocked.Increment(ref version);
            cleanup = false;

            Dictionary<int, int> d = BFS_algorithm(source, ver);

            if (Interlocked.Decrement(ref bfsCnt) == 0)
            {
                cleanup = true;
            }

            CleanGraph(ver);

            return d;
        }
    }
}