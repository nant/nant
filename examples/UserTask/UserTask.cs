using SourceForge.NAnt;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.SampleTask {

    [TaskName("usertask")]
    public class TestTask : Task {

        string _message = null;

        [TaskAttribute("message", Required=true)]
        public string FileName {
            get { return _message; }
            set { _message = value; }
        }

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + _message.ToUpper());
        }
    }
}