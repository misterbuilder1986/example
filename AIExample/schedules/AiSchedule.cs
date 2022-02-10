using System;
using System.Collections.Generic;

namespace engine.core.ai
{
    /// <summary>
    /// Class represent base AI schedule
    /// that process all TASKS during one AI schedule
    /// </summary>
    public class AiSchedule
    {
        protected bool _allFinished;
        protected List<String> _interrupts = new List<String>();
        protected List<String> _negInterrupts = new List<String>();
        protected List<AiTask> _tasks = new List<AiTask>();
        protected AiContext _context;

        public AiContext context
        {
            get
            {
                return _context;
            }
        }

        public AiSchedule(AiContext context)
        {
            _context = context;
        }

        public bool isCompleted(AiContext context)
        {
            List<String> conditions = context.conditions;
            for (int i = 0; i < conditions.Count; ++i)
            {
                if (_interrupts.IndexOf(conditions[i]) >= 0)
                    return true;
            }

            for (int i = 0; i < _negInterrupts.Count; ++i)
            {
                if (conditions.IndexOf(_negInterrupts[i]) == -1)
                    return true;
            }

            return _allFinished;
        }

        public bool execute(AiContext context)
        {
            // run tasks
            _allFinished = true;
            for (int i = 0; i < _tasks.Count; ++i)
            {
                if (!_tasks[i].execute(context))
                {
                    _allFinished = false;
                    break;
                }
            }

            return _allFinished;
        }

        public void addInterrupts(List<String> interrupts, bool interruptIfNotTrue = false)
        {
            for (int i = 0; i < interrupts.Count; ++i)
            {
                if (interruptIfNotTrue)
                    _negInterrupts.Add(interrupts[i]);
                else
                    _interrupts.Add(interrupts[i]);
            }
        }

        public virtual void addTasks(List<AiTask> tasks)
        {
            for (int i = 0; i < tasks.Count; ++i)
            {
                AiTask task = tasks[i];
                task._schedule = this;
                _tasks.Add(task);

            }
        }
    }
}
