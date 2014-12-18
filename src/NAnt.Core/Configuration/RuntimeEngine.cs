using System;
using System.Globalization;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class RuntimeEngine : Element {
        private FileInfo _program;
        private ArgumentCollection _arguments = new ArgumentCollection();

        [TaskAttribute ("program", Required=true)]
        public FileInfo Program {
            get { return _program; }
            set { _program = value; }
        }

        /// <summary>
        /// The command-line arguments for the runtime engine.
        /// </summary>
        [BuildElementArray("arg")]
        public ArgumentCollection Arguments {
            get { return _arguments; }
        }

        protected override void Initialize() {
            base.Initialize ();

            if (Program != null & !Program.Exists) {
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture, "Runtime engine '{0}'" +
                    " does not exist.", Program.FullName));
            }
        }
    }
}
