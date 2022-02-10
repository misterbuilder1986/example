using System;
using System.Collections.Generic;
using engine.core.events;
using engine.core.gameobject.animation;
using engine.core.gameobject.objects;
using engine.core.utils;
using royalsociety.gameobjects.objects;

namespace engine.core.ai
{
    /// <summary>
    /// Class represent AI logic for some animal
    /// Motions: Just idles, Just Wander(Some idles and GoToPositions)
    /// Ani AI schedules could be interapted by Dragging/Some State changes for Animal/Colliding 
    /// </summary>
    class AnimalAi : CharacterAi
    {
        private static readonly string CONDITION_IN_DRAG = "inDrag";
        private static readonly string CONDITION_STATE_CHANGED = "stateChanged";
        private static readonly string ON_DRAG_COLLIDE = "onDragCollide";

        private static readonly Random random = new Random();

        private AnimalBase _animal;
        private bool isCompletedAnimDragStart { get; set; }
        private bool isStateChanged;
        private bool prevStateWait;
        private bool someThingDragged = false;
        private bool onDragCollide = false;

        public AnimalAi(CharacterObjectBase character) : base(character)
        {
            _animal = character as AnimalBase;
            _animal.onStateChanged += _animal_onStateChanged;
            _animal.onDragCollide += _animal_onDragCollide;
            GameEventManager.instance.onSomeThingDragged += Instance_onSomeThingDragged;
        }

        private void _animal_onDragCollide(gameobject.core.ObjectBase obj)
        {
            onDragCollide = true;
        }

        private void Instance_onSomeThingDragged()
        {
            someThingDragged = true;
        }

        private void _animal_onStateChanged(gameobject.core.ObjectBase obj)
        {
            isStateChanged = true;
            prevStateWait = _animal.previousState == AnimalBase.STATE_WAIT;
        }

        protected override void gatherConditions(AiContext context)
        {
            if (_animal != null && _animal.isInDrag)
            {
                context.conditions.Add(CONDITION_IN_DRAG);
            }

            if (isStateChanged)
            {
                context.conditions.Add(CONDITION_STATE_CHANGED);
            }

            if (onDragCollide && _animal.isWorking)
            {
                context.conditions.Add(ON_DRAG_COLLIDE);
            }
        }

        protected override AiSchedule selectNewSchedule(AiContext context)
        {
            if (_animal == null || _animal.model == null)
            {
                return null;
            }

            if (isStateChanged)
            {
                isStateChanged = false;
                if (_animal.isMoving)
                {
                    _animal.stop();
                }
            }

            if (onDragCollide)
            {
                onDragCollide = false;
                if (_animal.isMoving)
                {
                    _animal.stopMove();
                }

                if (_animal.isWorking)
                {
                    return createAwaySchedule(context);
                }
            }

            // если в процессе производсва - гуляем
            if (_animal.isWorking && !_animal.isInDrag)
            {
                // если дом позиция у животного занята - найдем другую
                if (someThingDragged && _animal.needChangeHome())
                {
                    Log.i($"Current Home settings: {_animal.home} {_animal.needChangeHome()}");
                }
                return createWanderSchedule(context, someThingDragged && _animal.needChangeHome());
            }

            // если режим айдла - воиспроизводим его
            return createIdleSchedule(context);
        }

        private AiCharacterSchedule createAwaySchedule(AiContext context)
        {
            //Log.i("AnimalAi->createAwaySchedule");
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);
            List<AiTask> tasks = new List<AiTask>();

            List<(int x, int y, int distance)> cells = GameContext.world.map.findContinuousPassableCellsInARadius(_animal, _animal.meta.movementsRadius);
            if (cells.Count > 0)
            {
                (int x, int y, int distance) destinationCell = cells[random.Next(cells.Count)];
                tasks.Add(new AiTaskGoToPosition(new Vec2(destinationCell.x, destinationCell.y), true, true));
            }

            schedule.addTasks(tasks);
            schedule.addInterrupts(new List<string> { CONDITION_STATE_CHANGED });
            schedule.addInterrupts(new List<string> { CONDITION_IN_DRAG });
            schedule.addInterrupts(new List<string> { ON_DRAG_COLLIDE });

            return schedule;
        }

        private AiCharacterSchedule createWanderSchedule(AiContext context, bool savePosToServer = false)
        {
            //Log.i("AnimalAi->createWanderSchedule");
            someThingDragged = false;
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);
            List<AiTask> tasks = new List<AiTask>();

            if (isCompletedAnimDragStart)
            {
                isCompletedAnimDragStart = false;
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.DRAG_FINISH, 0, 1));
            }
            else
            if (prevStateWait && _animal.isWorking && _animal.model.AnimationPlayer.hasAnimation(AnimationNames.SLEEP_IDLE))
            {
                prevStateWait = false;
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.EAT, 0, 1));
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.JUMP, 0, 2));
            }

            tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE, 0, 3));

            List<(int x, int y, int distance)> cells = GameContext.world.map.findContinuousPassableCellsInARadius(_animal, _animal.meta.movementsRadius);
            if (cells.Count > 1)
            {
                (int x, int y, int distance) destinationCell = cells[random.Next(cells.Count)];
                tasks.Add(new AiTaskGoToPosition(new Vec2(destinationCell.x, destinationCell.y), true, random.NextDouble() <= 0.2, savePosToServer));
            }

            schedule.addTasks(tasks);
            schedule.addInterrupts(new List<string> { CONDITION_STATE_CHANGED });
            schedule.addInterrupts(new List<string> { CONDITION_IN_DRAG });
            schedule.addInterrupts(new List<string> { ON_DRAG_COLLIDE });

            return schedule;
        }

        private AiCharacterSchedule createIdleSchedule(AiContext context)
        {
            //Log.i("AnimalAi->createIdleSchedule");
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);
            List<AiTask> tasks = new List<AiTask>();

            if (_animal.isInDrag && _animal.model.AnimationPlayer.hasAnimation(AnimationNames.DRAG_IDLE))
            {
                schedule.addInterrupts(new List<string> { CONDITION_IN_DRAG }, true);
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.DRAG_START, 0, 1, AnimationNames.DRAG_IDLE));
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.DRAG_IDLE));
                isCompletedAnimDragStart = true;
                schedule.addTasks(tasks);
                return schedule;
            }

            schedule.addInterrupts(new List<string> { CONDITION_STATE_CHANGED });
            schedule.addInterrupts(new List<string> { CONDITION_IN_DRAG });
            schedule.addInterrupts(new List<string> { ON_DRAG_COLLIDE });

            if (isCompletedAnimDragStart)
            {
                isCompletedAnimDragStart = false;
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.DRAG_FINISH, 0, 1));
            }

            if (_animal.isProductCompleted && _animal.model.AnimationPlayer.hasAnimation(AnimationNames.SLEEP_IDLE))
            {
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.SLEEP_START, 0, 1, AnimationNames.SLEEP_IDLE));
                tasks.Add(new AiCharacterPlayAnimation(AnimationNames.SLEEP_IDLE));
                schedule.addTasks(tasks);
                return schedule;
            }

            tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE, 0, -1));
            schedule.addTasks(tasks);
            return schedule;
        }

        public override void cleanUp()
        {
            _animal.onStateChanged -= _animal_onStateChanged;
            GameEventManager.instance.onSomeThingDragged -= Instance_onSomeThingDragged;

            base.cleanUp();
        }
    }
}

