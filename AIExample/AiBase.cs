using engine.core.timers;
using System;

namespace engine.core.ai
{
    /// <summary>
    /// Class represents base abstract logic of some AI schedules
    /// Gathering AI interapt conditions
    /// Schedule new AI
    /// Destroy AI
    /// </summary>
    public abstract class AiBase
    {
        protected AiSchedule _currentSchedule;
        protected AiContext _context;
        protected int _runCount;
        private bool _isActive;
        protected float _startDelay;

        private string _intervalId;
        private string _startTimeoutId;

        public AiBase()
        {
            _context = new AiContext(this);
        }

        public bool isActive
        {
            get
            {
                return _isActive;
            }
        }

        public AiSchedule currentSchedule
        {
            get
            {
                return _currentSchedule;
            }

        }

        protected virtual void execute()
        {
            AiContext context = createContext();
            gatherConditions(context);

            if (_currentSchedule == null || _currentSchedule.isCompleted(context))
            {
                _currentSchedule = selectNewSchedule(context);
            }

            preScheduleThink();

            if (_currentSchedule != null)
            {
                _currentSchedule.execute(context);
            }

            postScheduleThink();
            //++_runCount;
        }

        public void run(float interval = 0.2f, int startDelay = 0)
        {
            _isActive = true;
            if (startDelay > 0)
                _startDelay = startDelay;
            else
                _startDelay = _startDelay <= 0 ? 1 : _startDelay;

            TimerHelper.clearTimeout(_startTimeoutId);
            _startTimeoutId = TimerHelper.setTimeout(() =>
            {
                _intervalId = TimerHelper.setTimeout(execute, interval, 0);
            }, _startDelay, 1);
        }

        private AiContext createContext()
        {
            AiContext context = new AiContext(this);
            context.global = _context.global;
            for (int i = 0; i < _context.signals.Count; ++i)
            {
                context.conditions.Add(_context.signals[i]);
            }

            _context = context;
            return context;
        }

        protected virtual AiSchedule selectNewSchedule(AiContext context)
        {
            throw new NotImplementedException();
        }

        protected virtual void gatherConditions(AiContext context)
        {
            // does nothing by default;
        }

        protected virtual void preScheduleThink()
        {
            // does nothing by default;
        }

        protected virtual void postScheduleThink()
        {
            // does nothing by default;
        }

        public virtual void cleanUp()
        {
            reset();
        }

        public virtual void reset()
        {
            _currentSchedule = null;
            TimerHelper.clearTimeout(_startTimeoutId);
            TimerHelper.clearTimeout(_intervalId);
            _intervalId = null;
            _startTimeoutId = null;
            //_runCount = 0;
            _isActive = false;
        }
    }
}
