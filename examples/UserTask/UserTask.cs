using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.SampleTask {

    [TaskName("usertask")]
    public class TestTask : Task {

        string _message = null;

        [TaskAttribute("message", Required=true)]
        public string FileName {
            get { return _message; }
            set { _message = value; }
        }

        protected override void ExecuteTask() {
            Log(Level.Info, LogPrefix + _message.ToUpper());
        }
    }
}
