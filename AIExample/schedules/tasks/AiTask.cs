namespace engine.core.ai
{
    /// <summary>
    /// Base AI task
    /// Derive this class if you want make some custom action
    /// ForExample: MoveToPosition, PlaySomeAnimation and etc
    /// </summary>
    public class AiTask
    {
        public AiSchedule _schedule;

        public AiTask(AiSchedule schedule = null)
        {
            _schedule = schedule;
        }

        public AiSchedule schedule
        {
            get
            {
                return _schedule;
            }
        }

        public virtual bool execute(AiContext context)
        {
            return true;
        }
    }
}
