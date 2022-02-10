using engine.core.gameobject.objects;
using System.Collections.Generic;

namespace engine.core.ai
{
    /// Class represent base AI schedule
    /// Private class for Character (Avatars) schedule
    /// </summary>
    class AiCharacterSchedule : AiSchedule
    {
        protected CharacterObjectBase _character;

        public AiCharacterSchedule(AiContext context, CharacterObjectBase character) : base(context)
        {
            _character = character;
        }

        public override void addTasks(List<AiTask> tasks)
        {
            for (int i = 0; i < tasks.Count; ++i)
            {
                AiCharacterTask task = tasks[i] as AiCharacterTask;
                if (task != null) task.character = _character;
            }

            base.addTasks(tasks);
        }
    }
}
