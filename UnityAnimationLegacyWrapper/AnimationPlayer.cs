using System;
using engine.core.interfaces;
using engine.core.utils.libs;

namespace engine.core.gameobject.animation
{
    public enum AnimationStopReason
    {
        undefined = -1,
        unplayable = 0, // анимации нету, вызывается сразу
        finished = 1, // проигррали всю анимацию и оснатовились
        restarted = 10, // вызвали play повторно до остановки
        aborted = 11 // вызвали stop
    }

    /// <summary>
    /// Class represents base abstract functions on Animation logic
    /// </summary>
    public abstract class AnimationPlayer
    {
        private const int MAX_DEPTH = 2;
        public const int REPEAT_INFINITE = -1;
        public const int TAKE_FROM_CONFIG = -2;

        public SimpleJSON.JSONObject meta { get; protected set; }

        public Boolean enabled { get; protected set; }
        public Boolean forceEnabled { get; protected set; }
        public AnimationMeta currentAnimation { get; private set; }

        protected string[] _playList;
        protected Action<string> _onCompleteHandler;
        protected int _seed;
       
        private int _animationsInProgress;
        private AnimationStopReason _currentStopReason = AnimationStopReason.undefined;
        private Action<AnimationStopReason> _recursiveAnimationCompletedCallback;
        protected ISIObject _target;

        public event Action onAnimationPlay;
        public void dispatchOnAnimationPlay() {
            onAnimationPlay?.Invoke();
        }

        public abstract void play(String name, int repeat = TAKE_FROM_CONFIG, Action<string> onCompleteCallback = null,
            Boolean force = false);

        public abstract void goTo(String name, Action<string> onComplete = null);

        public abstract void loop(String name);

        public abstract void playRandomWithPattern(string pattern, int repeat = AnimationPlayer.REPEAT_INFINITE,
            Action<string> onComplete = null);

        public abstract string[] getAnimations(String pattern);

        public abstract void stop();

        public abstract void reset();

        public abstract Boolean hasAnimation(String name);

        public abstract Boolean isPlaying(String name = null);

        protected abstract void playInternal(AnimationMeta clipMeta, int repeat, AnimationContext animationContext,
            Action<AnimationStopReason> onComplete);

        // если был добавлен колбэк в playInternal его надо вызвать с AnimationStopReason.aborted
        protected abstract void stopInternal();

        protected abstract void resetInternal(AnimationMeta clipMeta);

        protected abstract void goToLastFrameInternal(AnimationMeta clipMeta);

        protected void recursivePlay(AnimationMeta clipMeta, int repeat, Action<AnimationStopReason> onComplete,
            AnimationContext animationContext, int currentDepth = 0)
        {
//            Log.d($"calling recursivePlay for {_target.name}:{clipMeta.name}, current depth: {currentDepth}");
            currentAnimation = clipMeta;
            if (_animationsInProgress > 0)
            {
                recursiveStop();
            }

            //Debug.Assert(_animationsInProgress == 0, "_animationsInProgress != 0");
            //Debug.Assert(_recursiveAnimationCompletedCallback == null, "_recursiveAnimationCompletedCallback != null");
            _animationsInProgress += 1;
            _recursiveAnimationCompletedCallback = onComplete;
            if (currentDepth <= MAX_DEPTH)
            {
                foreach (ISIObject child in _target.children)
                {
                    if (child.animationPlayer != null)
                    {
                        _animationsInProgress += 1;
                        child.animationPlayer.recursivePlay(clipMeta, repeat, onPartAnimationComplete, animationContext, currentDepth + 1);
                    }
                }
            }

            playInternal(clipMeta, repeat, animationContext, onPartAnimationComplete);
        }

        protected void recursiveStop(int currentDepth = 0)
        {
            if (currentDepth <= MAX_DEPTH)
            {
                foreach (ISIObject child in _target.children)
                {
                    child.animationPlayer.recursiveStop(currentDepth + 1);
                }
            }

            stopInternal();
        }

        protected void recursiveReset(AnimationMeta clipMeta, int currentDepth = 0)
        {
            if (currentDepth <= MAX_DEPTH)
            {
                foreach (ISIObject child in _target.children)
                {
                    child.animationPlayer.recursiveReset(clipMeta, currentDepth + 1);
                }
            }

            resetInternal(clipMeta);
        }

        protected void recursiveGoToLastFrame(AnimationMeta clipMeta, int currentDepth = 0)
        {
            if (currentDepth <= MAX_DEPTH)
            {
                foreach (ISIObject child in _target.children)
                {
                    child.animationPlayer.recursiveGoToLastFrame(clipMeta, currentDepth + 1);
                }
            }
            goToLastFrameInternal(clipMeta);
        }

        private void onPartAnimationComplete(AnimationStopReason reason)
        {
            //Debug.Assert(_animationsInProgress > 0);
            _animationsInProgress -= 1;
            if (_currentStopReason < reason)
            {
                _currentStopReason = reason;
            }

            if (_animationsInProgress == 0)
            {
                var tmp = _recursiveAnimationCompletedCallback;
                var r = _currentStopReason;
                currentAnimation = null;
                _currentStopReason = AnimationStopReason.undefined;
                _recursiveAnimationCompletedCallback = null;
                tmp?.Invoke(r);
            }
        }
        
        public virtual bool isPlayingPrerequisite
        {
            get
            {
                return false;
            }
        }
    }
}