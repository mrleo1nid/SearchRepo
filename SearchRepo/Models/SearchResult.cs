using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchRepo.Models
{
    public class SearchResult
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Directory { get; set; }
        public int Row { get; set; }
    }
}
