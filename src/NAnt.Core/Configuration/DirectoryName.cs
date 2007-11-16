using System;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration
{
    [Serializable]
    internal class DirectoryName : Element
	{
        private string _name;

        [TaskAttribute("name", Required=true)]
        public string DirName {
            get { return _name; }
            set { _name = value; }
        }
    }
}
