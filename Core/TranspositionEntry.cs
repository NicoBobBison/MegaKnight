using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaKnight.Core
{
    public enum NodeType
    {
        PVNode,
        AllNode,
        CutNode
    }
    internal class TranspositionEntry
    {
        public ulong HashKey;
        public int Depth;
        public int Evaluation;
        public NodeType NodeType;
        public Move BestMove;
        public int Age;
    }
}
