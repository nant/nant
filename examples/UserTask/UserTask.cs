namespace SourceForge.NAnt.SampleTask {

    using SourceForge.NAnt;

    [TaskName("usertask")]
    public class TestTask : Task {

        [TaskAttribute("message", Required=true)]
        string _message = null;

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + _message.ToUpper());
        }
    }
}
