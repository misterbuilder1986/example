using engine.core.gameobject.animation;
using System;

namespace engine.core.ai
{
    /// <summary>
    /// Example of AI: PlayAnimation
    /// Task running while character is playing some animation
    /// </summary>
    public class AiCharacterPlayAnimation : AiCharacterTask
    {
        private double _startTime;
        private double _duration;
        private string _animation;
        private bool _finished;
        private bool _started;
        private string _noInterrupt;
        private int _repeat;

        public AiCharacterPlayAnimation(string animation, double duration = 0, int repeats = AnimationPlayer.TAKE_FROM_CONFIG, string noInterrupt = null)
        {
            _animation = animation;
            _duration = duration * 1000;
            _noInterrupt = noInterrupt;
            _repeat = repeats;
        }

        public override bool execute(AiContext newContext)
        {
            if (_startTime <= 0)
            {
                if (character.isMoving) return false;

                if (_noInterrupt == null || character.model.AnimationPlayer.currentAnimation == null || character.model.AnimationPlayer.currentAnimation.name.IndexOf(_noInterrupt) == -1)
                {
                    if (_duration > 0)
                    {
                        //Log.i($"AnimalAi->execute1: {_animation} {_repeat}");
                        character.model.AnimationPlayer.playRandomWithPattern(_animation, _repeat);
                    }
                    else
                    {
                        //Log.i($"AnimalAi->execute2: {_animation} {_repeat}");
                        character.model.AnimationPlayer.playRandomWithPattern(_animation, _repeat, onComplete);
                    }
                }

                //character.model.AnimationPlayer.onAnimationStart += onAnimation;
                _startTime = DateTime.Now.Ticks;
                return false;
            }
            else
            {
                if (_duration > 0)
                {
                    return DateTime.Now.Ticks - _startTime > _duration;
                }
                else
                {
                    AnimationPlayer animation = character.model.AnimationPlayer;
                    //Log.i($"AnimalAi->execute3: {_animation} {_repeat} {_finished} {animation.currentAnimation} {animation.currentAnimation} {!animation.isPlayingPrerequisite} {!animation.isPlaying()}");

                    bool result = _finished
                                  || animation.currentAnimation == null
                                  || (animation.currentAnimation.name.IndexOf(_animation) != 0 &&
                                      !animation.isPlayingPrerequisite)
                                  || !animation.isPlaying();

                    return result;
                }

            }
        }

        private void onComplete(Object animation = null)
        {
            //Log.i("AiCharacterPlayAnimation->onComplete Animation:" + _animation);
            _finished = true;
        }
    }
}
