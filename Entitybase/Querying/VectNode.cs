using Shantiw.Data.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shantiw.Data.Querying
{
    public class VectNode
    {
        public int Id { get; private set; }

        public VectAssociation? VectAssociation { get; private set; }

        public VectNode? Parent { get; private set; }

        private readonly Dictionary<string, VectNode> _children = [];
        public IReadOnlyDictionary<string, VectNode> Children => _children;

        private VectNode()
        {
            Id = 0;
            VectAssociation = null;
            Parent = null;
        }

        private VectNode(VectNode? parent, VectAssociation[] vector, int index, Func<int> getSequence)
        {
            Id = getSequence();
            Parent = parent;
            VectAssociation = vector[index];
            for (int i = index + 1; i < vector.Length; i++)
            {
                _children.Add(vector[i].Name, new VectNode(this, vector, i, getSequence));
            }
        }

        internal static VectNode CreateRoot()
        {
            return new VectNode();
        }

        internal static void Build(VectAssociation[] vector, VectNode root, Func<int> getSequence, out int lastId)
        {
            int index = 0;
            VectNode node = root;
            while (node._children.TryGetValue(vector[index].Name, out VectNode? subNode))
            {
                node = subNode;
                lastId = node.Id;
                index++;
                if (index == vector.Length) return;
            }
            lastId = -1;
            node._children.Add(vector[index].Name, new VectNode(node, vector, index, getSequence));
        }

    }
}
