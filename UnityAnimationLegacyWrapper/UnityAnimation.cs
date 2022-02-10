using System;
using System.Collections.Generic;
using System.Linq;
using engine.core;
using engine.core.gameobject.animation;
using engine.core.interfaces;
using engine.core.sound;
using engine.core.utils;
using engine.core.utils.libs;
using unitywrapper.utils;
using UnityEngine;

namespace unitywrapper.siobjects.animations
{
    /// <summary>
    /// Class represents wrapper on Animation Unity components
    /// </summary>
    public class UnityAnimationEntity
    {
        // свойства из модели
        private readonly ISIObject _gameObject;
        private readonly Animation _animComponent;
        private readonly AnimationState _animState;
        private readonly AnimationClip _clip;
        private readonly float _modelSpeed;
        private readonly ObjectBaseAnimationComponent _callbackComponent;
        public string name { get; }

        //задается при проигрывании
        private Action<AnimationStopReason, string> _completeHandler;
        private int _repeat;
        private AnimationMeta _meta;

        public bool isLoaded => true;
        public bool isLoading => false;
        public bool isPlaying => _animComponent.isPlaying && _animComponent.clip == _clip;
        public bool isInitialized => true;

        public UnityAnimationEntity(string name, Animation animComponent, AnimationState state, ObjectBaseAnimationComponent callbackComponent,
            ISIObject gameObject)
        {
            this.name = name;
            this._animComponent = animComponent;
            this._callbackComponent = callbackComponent;
            this._animState = state;
            this._gameObject = gameObject;
            this._clip = state.clip;
            this._clip.legacy = true;
            this._modelSpeed = state.speed;

            // Подписываем на событие от юнити на завершение анимации
            if (_clip.events.Count((evt) => evt.functionName == "onAnimationCompleteOnce") == 0)
            {
                var evt = new AnimationEvent
                {
                    time = _clip.length,
                    functionName = "onAnimationCompleteOnce"
                };
                _clip.AddEvent(evt);
            }
        }

        public void play(
            AnimationMeta meta, int repeat, bool needRegisterFrameSounds,
            Action<AnimationStopReason, string> onComplete)
        {
            // если проигрывали до этого анимацию
            // растартим ее
            if (isPlaying)
            {
                var tmp = _completeHandler;
                _completeHandler = null;
                tmp?.Invoke(AnimationStopReason.restarted, name);
            }

            _meta = meta;
            _callbackComponent.AnimationEndCallback = onPlayOnceComplete;
            _repeat = repeat;
            _completeHandler = onComplete;
            _animState.speed = meta.speed * _modelSpeed;
            _animState.wrapMode = meta.pingPong ? WrapMode.PingPong : WrapMode.Once;;
            _animComponent.clip = _clip;

            // если на определенный фрейм анимации нужно производить звук
            // зарегистрируем его через мету
            if (needRegisterFrameSounds)
            {
                this.registerAnimationFrameSounds(meta);
            }

            _animComponent.Play(name);

            this.playSound();
        }

        #region sound frame logic

        // регистрация проигрывание звука на определенный фрейм анимации
        private void registerAnimationFrameSounds(AnimationMeta animationMeta)
        {
            if (animationMeta.soundFrame == null)
            {
                return;
            }

            List<int> frameNumbers = animationMeta.soundFrame["frames"].Value
                .Split(',')
                .Select(NumberHelper.IntParse)
                .ToList();

            this._callbackComponent.onSoundFrameEnterCallback = this.onSoundFrameEnter;

            foreach (int frameNumber in frameNumbers)
            {
                float frameTime = this.getTimeForFrameNumber(frameNumber);
                string frameInfo = frameNumber + "#" + animationMeta.soundFrame["names"].Value;

                if (this._clip.events.Any(e => e.stringParameter == frameInfo))
                {
                    continue;
                }

                this._clip.AddEvent(new AnimationEvent
                {
                    time = frameTime,
                    functionName = nameof(this.onSoundFrameEnter),
                    stringParameter = frameInfo,
                });
            }
        }

        private void onSoundFrameEnter(string frameInfo)
        {
            if (frameInfo == null)
            {
                return;
            }

            string[] frameInfoParts = frameInfo.Split('#');

            if (frameInfoParts.Length < 2)
            {
                return;
            }

            string[] soundNames = frameInfoParts[1].Split(',');

            if (soundNames.Length == 0)
            {
                return;
            }

            int soundIndex = GameContext.gameManager.randomManager.getUnsafeRandom(0, soundNames.Length - 1);
            SoundEntity sound = Sounds.getByName(soundNames[soundIndex]);

            if (sound == null)
            {
                return;
            }

            GameContext.sound.playSoundOnObject(this._gameObject, sound, 1, true);
        }

        private float getTimeForFrameNumber(int frameNumber)
        {
            return frameNumber / this._clip.frameRate;
        }

        private void playSound()
        {
            SimpleJSON.JSONNode soundMeta = this._meta.sound;

            if (soundMeta == null)
            {
                return;
            }

            if (soundMeta.IsString)
            {
                GameContext.sound.playSoundOnObject(
                    this._gameObject, Sounds.getByName(soundMeta.Value), fadeOutWithDistance: true);
            }
            else if (soundMeta.IsObject)
            {
                string animationName = this._meta.name;
                SimpleJSON.JSONObject soundData = soundMeta.AsObject;

                if (soundData["chance"] == null)
                {
                    Log.w($"Animation '{animationName}': " +
                          "no 'chance' property is specified for 'sound' property so it will never play.");
                    return;
                }

                double playChance = soundData["chance"].AsDouble;
                double randomRoll = GameContext.gameManager.randomManager.getUnsafeRandom();

                if (randomRoll >= playChance)
                {
                    return;
                }

                if (soundData["name"] == null)
                {
                    Log.w($"Animation '{animationName}': " +
                          "no 'name' property is specified for 'sound' property so sound will not play.");
                    return;
                }

                string[] soundNames = soundData["name"].Value.Split(',');

                float soundVolume = soundData["volume"] != null
                    ? soundData["volume"].AsFloat
                    : 0;

                int soundIndex = GameContext.gameManager.randomManager.getUnsafeRandom(0, soundNames.Length - 1);

                GameContext.sound.playSoundOnObject(
                    this._gameObject, Sounds.getByName(soundNames[soundIndex]), soundVolume, true);
            }
            else
            {
                Log.w($"Unable to play sound for animation '{this._meta.name}': unknown format of 'sound' property.");
            }
        }

        #endregion sound frame logic

        private void onPlayOnceComplete()
        {
            _callbackComponent.AnimationEndCallback = null;
            _repeat--;
            if (_repeat > 0)
            {
                _animComponent.Stop(name);
                play(_meta, _repeat, false, _completeHandler);
                return;
            }
            var onComplete = _completeHandler;
            _completeHandler = null;
            stopInternal();
            onComplete.Invoke(AnimationStopReason.finished, name);
        }

        private void stopInternal()
        {
            if (!isPlaying)
            {
                return;
            }
         
            if (_meta.reset)
            {
                reset();
            }

            _animState.speed = 0;
            _animComponent.Stop(name);
        }

        public void stop()
        {
            stopInternal();
            var onComplete = _completeHandler;
            _completeHandler = null;
            onComplete?.Invoke(AnimationStopReason.aborted, name);
        }

        public void reset()
        {
            _animState.time = 0;
        }

        public void toLastFrame()
        {
            _animState.time = _animState.length;
        }

    }
}
