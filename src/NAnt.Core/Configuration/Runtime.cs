using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class Runtime : Element {
        private ManagedExecutionModes _modes = new ManagedExecutionModes ();
        private DirList _probingPaths = new DirList();

        [BuildElement("probing-paths")]
        public DirList ProbingPaths {
            get { return _probingPaths; }
        }

        [BuildElement("modes")]
        public ManagedExecutionModes Modes {
            get { return _modes; }
        }
    }
}
