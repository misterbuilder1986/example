using System;
using System.Collections.Generic;
using engine.core;
using engine.core.gameobject.animation;
using engine.core.utils;
using engine.core.utils.libs;
using unitywrapper.utils;
using UnityEngine;

namespace unitywrapper.siobjects.animations
{
    /// <summary>
    /// Class represents Animation state data
    /// </summary>
    internal class AnimationAndMeta
    {
        public AnimationMeta meta;
        public UnityAnimationEntity animation;
    }

    /// <summary>
    /// Class represent logic of playing some animations
    /// Animation Meta is needed
    /// example:
    /// "animations": {
    ///	"idle1": {
    ///		"embedded": true,
    ///		"frameStart": 0,
    ///		"frameEnd": 80,
    ///		"repeat": -1
    ///},
    /// </summary>
    public class UnityAnimationPlayer : AnimationPlayer
    {
        private static System.Random random = new System.Random();
        private static int count = 0; // seed number to random idle animations on the same models
        private ObjectBaseAnimationComponent _callbackComponent;
        private Animation _animationComponent;
        private Dictionary<string, UnityAnimationEntity> _objectAnimations = new Dictionary<string, UnityAnimationEntity>();
        private Dictionary<string, AnimationAndMeta> _allAnimations;

        public UnityAnimationPlayer(Unity3DSIObject gameObject)
        {
            enabled = false;
            _target = gameObject;
            _animationComponent = ((GameObject)_target.rawData).GetComponent<Animation>();
            if (_animationComponent != null)
            {
                _animationComponent.Stop();
                _animationComponent.playAutomatically = false;
                _callbackComponent =
                    ((GameObject)_target.rawData).getOrAddComponent<ObjectBaseAnimationComponent>();
                foreach (AnimationState state in _animationComponent)
                {
                    _objectAnimations[state.name] = new UnityAnimationEntity(state.name, _animationComponent, state, _callbackComponent, gameObject);
                }
            }
        }

        public void init(SimpleJSON.JSONObject allAnimationsMeta)
        {
            this.meta = allAnimationsMeta;
            _seed = ++count;
            _allAnimations = new Dictionary<string, AnimationAndMeta>();

            // проходим через json meta и регистрируем AnimationMeta 
            foreach (KeyValuePair<string, SimpleJSON.JSONNode> pair in meta)
            {
                UnityAnimationEntity a;
                _objectAnimations.TryGetValue(pair.Key, out a);
                AnimationMeta m = new AnimationMeta(pair.Key, pair.Value.AsObject);

                _allAnimations[pair.Key] = new AnimationAndMeta { meta = m, animation = a };
            }

            enabled = true;
        }

        // проигрывает звук в зависимости от настроек анимации в мета файле
        public override void play(String name, int repeat = TAKE_FROM_CONFIG, Action<string> onCompleteCallback = null, Boolean force = false)
        {
            if (!enabled)
            {
                return;
            }
            bool sameAnimation = (currentAnimation?.name == name);

            recursiveStop();

            if (GameSettings.instance.disableAnimations && !forceEnabled && !force)
            {
                return;
            }

            var animToPlay = getAnimation(name);
            if (animToPlay == null)
            {
                return;
            }

            _onCompleteHandler = onCompleteCallback;

            if (repeat == TAKE_FROM_CONFIG) repeat = animToPlay.meta.repeat;
            if (repeat == REPEAT_INFINITE) repeat = int.MaxValue;

            // reset if this is another animation or we are not looping
            if (!sameAnimation || repeat == 1)
            {
                recursiveReset(animToPlay.meta);
            }
            recursivePlay(animToPlay.meta, repeat, (reason) =>
            {
                if (reason == AnimationStopReason.finished) // вызываем onComplete только если анимашка доигралась сама (не прервали, не перезапустили, и она есть вообще)
                {
                    animationCompleteHandler(animToPlay);
                }
                else
                {
                    // Log.d($"AnimationPlayer for {_target.name}: onComplete({name}) not called, animation was stopped({reason})");
                }
            }, AnimationContext.createNew());

        }

        // пропускает анимацию и сразу запускает следующую. Имя плохое.
        public override void goTo(String name, Action<string> onComplete = null)
        {
            if (!enabled)
            {
                return;
            }

            recursiveStop();

            var animToPlay = getAnimation(name);
            if (animToPlay != null)
            {
                recursiveGoToLastFrame(animToPlay.meta);
                _onCompleteHandler = onComplete;
                animationCompleteHandler(animToPlay);
            }
        }

        public override void loop(String name)
        {
            play(name, int.MaxValue);
        }

        public override void playRandomWithPattern(string pattern, int repeat = AnimationPlayer.REPEAT_INFINITE, Action<string> onComplete = null)
        {
            if (!enabled)
            {
                return;
            }

            string[] animations = getAnimations(pattern);
            playRandom(animations, repeat, onComplete);
        }

        private void playRandom(string[] names, int repeat = REPEAT_INFINITE, Action<string> onComplete = null)
        {
            if (!enabled)
            {
                return;
            }

            if (names == null || names.Length == 0 || GameSettings.instance.disableAnimations && !forceEnabled)
            {
                stop(); // complete stop
                return;
            }
            else
            {
                stopCurrent();
            }

            if (names.Length == 1)
            {
                play(names[0], repeat, onComplete);
                return;
            }

            int rand = GameContext.gameManager.randomManager.getUnsafeRandom(0, names.Length);

            var animToPlay = getAnimation(names[rand]);

            if (animToPlay.meta.chance * 1.15 < (float)GameContext.gameManager.randomManager.getUnsafeRandom(0,100)/100f)
            {
                animToPlay = getAnimation(names[0]);
            }

            _onCompleteHandler = onComplete;

            if (repeat == TAKE_FROM_CONFIG) repeat = animToPlay.meta.repeat;
            if (repeat == REPEAT_INFINITE) repeat = int.MaxValue;

            if (repeat >= 1)
            {
                _playList = names;
                recursiveReset(animToPlay.meta);
                recursivePlay(animToPlay.meta, 1, (stopReason) =>
                {
                    if (stopReason == AnimationStopReason.finished)
                    {
                        playRandom(_playList, repeat - 1, _onCompleteHandler);
                    }

                }, AnimationContext.createNew());
            }
            else
            {
                _onCompleteHandler?.Invoke(animToPlay.meta.name);
                _onCompleteHandler = null;
                _playList = null;
            }
        }

        private void animationCompleteHandler(AnimationAndMeta anim)
        {
            _onCompleteHandler?.Invoke(anim.meta.name);
            _onCompleteHandler = null;
            if (anim.meta.next != null)
            {
                string next = anim.meta.next;
                _playList = null;
                play(next);
            }
        }

        public override string[] getAnimations(String pattern)
        {
            List<string> result = new List<string>();
            foreach (var animToPlay in _allAnimations.Values)
            {
                if (pattern == null || animToPlay.meta.name.StartsWith(pattern))
                {
                    result.Add(animToPlay.meta.name);
                }
            }
            return result.ToArray();
        }

        public override void stop()
        {
            stopCurrent();
            _onCompleteHandler = null;
            _playList = null;
        }

        public override void reset()
        {
            if (currentAnimation != null)
            {
                recursiveReset(currentAnimation);
            }
        }

        private void stopCurrent()
        {
            if (currentAnimation != null)
            {
                recursiveStop();
            }
        }

        public override bool hasAnimation(String name)
        {
            return _allAnimations != null && getAnimation(name) != null;
        }

        public override bool isPlaying(String name = null)
        {
            return currentAnimation != null
                && (name == null || currentAnimation.name == name)
                && _objectAnimations.ContainsKey(currentAnimation.name)
                && _objectAnimations[currentAnimation.name].isPlaying;
        }

        protected override void playInternal(AnimationMeta clipMeta, int repeat, AnimationContext animationContext,
            Action<AnimationStopReason> onComplete)
        {
            if (_objectAnimations.ContainsKey(clipMeta.name))
            {
                _objectAnimations[clipMeta.name].play(clipMeta, repeat, animationContext.needRegisterAnimationSounds,
                    (reason, name) => onComplete?.Invoke(reason));
                animationContext.needRegisterAnimationSounds = false;
            }
            else
            {
                onComplete?.Invoke(AnimationStopReason.unplayable);
            }
        }

        protected override void stopInternal()
        {
            if (currentAnimation != null && _objectAnimations.ContainsKey(currentAnimation.name))
            {
                _objectAnimations[currentAnimation.name].stop();
            }
        }

        protected override void resetInternal(AnimationMeta clipMeta)
        {
            if (_objectAnimations.ContainsKey(clipMeta.name))
            {
                _objectAnimations[clipMeta.name].reset();
            }
        }

        protected override void goToLastFrameInternal(AnimationMeta clipMeta)
        {
            if (_objectAnimations.ContainsKey(clipMeta.name))
            {
                _objectAnimations[clipMeta.name].toLastFrame();
            }
        }

        private AnimationAndMeta getAnimation(String name)
        {
            if (!_allAnimations.ContainsKey(name))
            {
                return null;
            }
            return _allAnimations[name];
        }
    }
}
