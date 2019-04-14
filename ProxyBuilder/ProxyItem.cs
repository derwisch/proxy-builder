using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyBuilder
{
    public class ProxyItem
    {
        public int Count { get; set; }
        public string Name { get; set; }
        public string SelectedSet { get; set; }
        public IEnumerable<string> PossibleSets { get; set; }
    }
}
