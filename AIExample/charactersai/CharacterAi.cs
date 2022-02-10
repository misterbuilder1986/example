using engine.core.gameobject.objects;

namespace engine.core.ai
{
    public abstract class CharacterAi : AiBase
    {
        protected CharacterObjectBase _character;

        public CharacterAi(CharacterObjectBase character)
        {
            _character = character;
        }
    }
}
