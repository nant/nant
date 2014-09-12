using System;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class ManagedExecutionModes : Element {
        private ManagedExecutionMode _autoMode;
        private ManagedExecutionMode _strictMode;

        [BuildElement("auto")]
        public ManagedExecutionMode Auto {
            get { return _autoMode; }
            set { _autoMode = value; }
        }

        [BuildElement("strict")]
        public ManagedExecutionMode Strict {
            get { return _strictMode; }
            set { _strictMode = value; }
        }

        public ManagedExecutionMode GetExecutionMode (ManagedExecution managed) {
            switch (managed) {
                case ManagedExecution.Default:
                    return null;
                case ManagedExecution.Auto:
                    return Auto;
                case ManagedExecution.Strict:
                    if (Strict != null)
                        return Strict;
                    return Auto;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
