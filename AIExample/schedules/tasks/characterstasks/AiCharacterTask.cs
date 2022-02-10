using engine.core.gameobject.objects;

namespace engine.core.ai
{
    public abstract class AiCharacterTask : AiTask
    {
        public CharacterObjectBase character;

        public AiCharacterTask(AiSchedule schedule = null) : base(schedule)
        {

        }
    }
}
