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

        public VectAssociation VectAssociation { get; private set; }

        public VectNode? Parent { get; private set; }

        private readonly Dictionary<string, VectNode> _children = [];
        public IReadOnlyDictionary<string, VectNode> Children => _children;

        internal static void Build(VectAssociation[] vector, VectNode root, Func<int> getSequence)
        {
            if (vector.Length == 0) return;
            if (vector[0].Name != root.VectAssociation.Name) return;
            if (vector.Length == 1) return;

            int index = 1;
            VectNode node = root;
            while (node._children.TryGetValue(vector[index].Name, out VectNode? subNode))
            {
                node = subNode;
                index++;
                if (index == vector.Length) return;
            }
            node._children.Add(vector[index].Name, new VectNode(node, vector, index, getSequence));
        }

        internal VectNode(VectNode? parent, VectAssociation[] vector, int index, Func<int> getSequence)
        {
            Id = getSequence();
            Parent = parent;
            VectAssociation = vector[index];
            for (int i = index + 1; i < vector.Length; i++)
            {
                _children.Add(vector[i].Name, new VectNode(this, vector, i, getSequence));
            }
        }

    }
}
