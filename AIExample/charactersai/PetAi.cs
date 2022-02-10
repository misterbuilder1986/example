using engine.core.gameobject.animation;
using engine.core.gameobject.objects;
using engine.core.timers;
using engine.core.utils;
using System;
using System.Collections.Generic;
using engine.core.gameobject.core;

namespace engine.core.ai
{
    /// <summary>
    /// Class represent AI logic for some home animal
    /// Motions: Just idles, Just Wander(Some idles and GoToPositions), Go To Bowl and eat
    /// Ani AI schedules could be interrapted by Dragging/Some State changes for Animal/Colliding
    /// also by some user actin: forexample: go to home action.
    /// </summary>
    class PetAi : CharacterAi
    {

        private static readonly string CONDITION_PET_HOUSE_PRESENT = "petHousePresent";
        private static readonly string CONDITION_GO_HOME = "goHome";
        private static readonly string CONDITION_FOOD_IN_BOWL_JUST_ADDED = "foodAdded";

        private AnimationMeta[] _animations;
        private bool _isHungry;
        private PetHouseBase _petHouse;
        private PetAnimalBase _pet;
        private bool _callPets;
        private bool _foodInBowlJustAdded;

        public PetAi(CharacterObjectBase character, AnimationMeta[] animations = null) : base(character)
        {
            _pet = character as PetAnimalBase;
            _animations = animations;
            _startDelay = 1800; // pet ai starts with some delay
            GameContext.eventManager.onCallPets += onCallPets;
            GameContext.eventManager.onAddedFoodToBowl += onAddedFoodToBowl;
        }

        protected override void gatherConditions(AiContext context) {
            _isHungry = _pet != null && _pet.isHUNGRY;

            if (_petHouse == null) {
                _petHouse = _pet.getHouse();
                if (_petHouse != null)
                    context.conditions.Add(CONDITION_PET_HOUSE_PRESENT);
            }

            if (_petHouse != null && !_petHouse.isInDrag && (_callPets || _isHungry)) {

                if (_foodInBowlJustAdded) {
                    context.conditions.Add(CONDITION_FOOD_IN_BOWL_JUST_ADDED);
                    _foodInBowlJustAdded = false;
                }

                context.conditions.Add(CONDITION_GO_HOME);
            }
        }

        protected override AiSchedule selectNewSchedule(AiContext context)
        {
            if (_petHouse != null && !_petHouse.isInDrag) {
                // если мы голодные --
                if (_isHungry) {
                    // если есть миска с едой, и она не занята
                    int bowl = (int)_petHouse.acceptBowlsWithFood(_pet);
                    if (bowl != -1) {
                        return createGoToBowlSchedule(context, bowl);
                    } else {
                        return createGoToPetHouseSchedule(context);
                    }
                }

                // если была команда домой
                if (_callPets) {
                    return createGoToPetHouseSchedule(context);
                }

            }

            // иначе всегда просто гуляем
            return createWanderSchedule(context);
        }

        private AiCharacterSchedule createGoToBowlSchedule(AiContext context, int bowl) {
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);
            List<AiTask> tasks = new List<AiTask>();
            Vec3 petPos = _pet.view.getPosition();

            //log.i("petPos",_pet,petPos);

            Vec3 localPos = _petHouse.getBowlViewByNumber(bowl).getPosition();
            localPos.x += 0.65f;
            localPos.z -= 0.65f;
            Vec3 beforeBowlView = _petHouse.view.localToGlobal(localPos);

            //log.i("bowlView",_pet,beforeBowlView,bowl);

            float dist = DistanceHelper.getSquareVectorDist(petPos, beforeBowlView);
            //log.i("dist",_pet,dist);

            if (dist < 1) {
                // near to bowl
                localPos = _petHouse.getBowlViewByNumber(bowl).getPosition();
                Vec3 bowlView = _petHouse.view.localToGlobal(localPos);
                _pet.lookAtPosition(bowlView.x, bowlView.z);

                // посылаем команду кушать и проигрываем анимацию
                tasks.Add(new AiCharacterPlayAnimation(CharacterAnimations.EATING, 0, 1));
                _pet.doEat();
            }
            else {
                tasks.Add(new AiTaskGoToPosition(new Vec2(beforeBowlView.x, beforeBowlView.z), true, true));
            }

            schedule.addTasks(tasks);
            return schedule;
        }

        private AiCharacterSchedule createGoToPetHouseSchedule(AiContext context)
        {
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);
            List<AiTask> tasks = new List<AiTask>();
            float dist = 9999;

            //if (_pet.slot != null) {
            //    Vec3 petPos = _pet.view3d.getPosition();
            //    Vec3 slotPos = _pet.getGlobalPosition();
            //    dist = DistanceHelper.getSquareVectorDist(petPos, slotPos);
            //}
            Random rand = new Random();
            if (dist < 1) {
                // near to house
                if (rand.NextDouble() < 0.14) {
                    // randomly wander sometimes
                    tasks.Add(new AiTaskWander(3));
                    if (rand.NextDouble() < 0.3) {
                        tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE_SPECIAL, 0, (int)(rand.NextDouble() * 3.1)));
                        tasks.Add(new AiTaskWander(3));
                    }
                    if (rand.NextDouble() < 0.2) {
                        tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE_SPECIAL, 0, 1));
                        tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE_SPECIAL, 0, 1));
                        tasks.Add(new AiTaskWander(3));
                    }
                    tasks.Add(new AiTaskWander(3));
                    tasks.Add(new AiCharacterPlayAnimation(AnimationNames.IDLE, 0, 1));
                    _callPets = false;
                }
                else if (_isHungry) {
                    tasks.Add(new AiCharacterPlayAnimation(CharacterAnimations.LAY_DOWN, 10, AnimationPlayer.TAKE_FROM_CONFIG, CharacterAnimations.LAY));
                    schedule.addInterrupts(new List<string> { CONDITION_FOOD_IN_BOWL_JUST_ADDED });
                }
                else {
                    tasks.Add(new AiCharacterPlayAnimation(CharacterAnimations.SIT_DOWN, 10, AnimationPlayer.TAKE_FROM_CONFIG, CharacterAnimations.SIT));
                }
            }
            else {
            //var slot:Slot = null;//_petHouse.addObjectToSlot(_pet);
            //if (slot) {
            //    var pos:Vector3D = _petHouse.view3d.localToGlobal(slot.position);
            //    tasks.push(new AiTaskGoToPosition(new Point(pos.x, pos.z), true, dist > 3));
            //}
            //else {
                    return createWanderSchedule(context);
              //  }
            }

            schedule.addTasks(tasks);
            schedule.addInterrupts(new List<string> { CONDITION_GO_HOME }, true);

            return schedule;
        }

        private AiCharacterSchedule createWanderSchedule(AiContext context) {
            AiCharacterSchedule schedule = new AiCharacterSchedule(context, _character);

            Vec2 nextPos = null;
            Random random = new Random();
            List<AiTask> tasks = new List<AiTask>();

            nextPos = null;//GameContext.world.map.getFreeRandomFieldPoint();
            if (nextPos != null)
                tasks.Add(new AiTaskGoToPosition(nextPos, true, random.NextDouble() <= 0.08));

            int moves = (int)(random.NextDouble() * 4.1) + 3;
            string animation;

            for (int i = 0; i < moves; ++i) {
                double rand = random.NextDouble();
                animation = null;

                if (_animations != null) {
                    for (int aIndex = 0; aIndex < _animations.Length; ++aIndex) {
                        AnimationMeta aMeta = _animations[aIndex];
                        if (aMeta.chance >= rand) {
                            animation = aMeta.name;
                            break;
                        }
                    }
                }

                if (animation == null) {
                    if (random.NextDouble() >= 0.5)
                        animation = AnimationNames.IDLE_SPECIAL;
                    else {
                        rand = random.NextDouble();
                        if (rand > 0.95)
                            animation = CharacterAnimations.LAY_DOWN;
                        else if (rand > 0.62)
                            animation = CharacterAnimations.SIT_DOWN;
                        else
                            animation = AnimationNames.IDLE;
                    }
                }

                if (animation == AnimationNames.IDLE) {
                    tasks.Add(new AiCharacterPlayAnimation(animation, 0, (int)(random.NextDouble() * 3.1)));
                }
                else if (animation == CharacterAnimations.SIT_DOWN || animation == CharacterAnimations.LAY_DOWN) {
                    tasks.Add(new AiCharacterPlayAnimation(animation, (int)(random.NextDouble() * 6.6 + 6)));
                }
                else {
                    if (rand > 0.3)
                        tasks.Add(new AiCharacterPlayAnimation(animation));
                    tasks.Add(new AiCharacterPlayAnimation(animation));
                }

                tasks.Add(new AiTaskWander(3));
            }

            schedule.addInterrupts(new List<string> { CONDITION_GO_HOME });
            if (_isHungry && _petHouse == null) {
                schedule.addInterrupts(new List<string> { CONDITION_PET_HOUSE_PRESENT });
            }

            schedule.addTasks(tasks);
            return schedule;
        }

        private void onCallPets(ObjectBase petHouse) {
            if (_petHouse != null && petHouse == _petHouse) {
                _callPets = true;
                TimerHelper.setTimeout(() => {
                    _callPets = false;
                }, 15000);
            }
        }

        private void onAddedFoodToBowl(ObjectBase petHouse) {
            if (_petHouse != null && petHouse == _petHouse) {
                _foodInBowlJustAdded = true;
            }
        }

        public override void cleanUp()
        {
            base.cleanUp();
            GameContext.eventManager.onAddedFoodToBowl -= onAddedFoodToBowl;
            GameContext.eventManager.onCallPets -= onCallPets;
        }
    }
}

