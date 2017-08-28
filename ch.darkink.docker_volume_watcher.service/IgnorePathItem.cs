using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.darkink.docker_volume_watcher.service {
    public class IgnorePathItem {

        public String Test { get; set; }
        public IgnorePathItemType Type { get; set; }
        public Object Custom { get; set; }
    }

    public enum IgnorePathItemType {
        Unknown,
        StartWith,
        EndWith,
        Regex
    }

}
