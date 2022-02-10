using engine.core.actionProcessor;
using engine.core.utils;

namespace engine.core.ai
{
    /// <summary>
    /// Example of AI: GoToPosition
    /// Task running while character is moving goto some position
    /// </summary>
    class AiTaskGoToPosition : AiCharacterTask
    {
        private Vec2 _currentWayPoint;
        private bool _wayPointReached;
        private bool _ignoreObstaclesAtPosition = true;
        private bool _run;
        private bool _savePosToServer;

        public AiTaskGoToPosition(Vec2 pos, bool ignoreObstaclesAtPosition = true, bool run = true, bool savePosToServer = false)
        {
            _currentWayPoint = pos;
            _ignoreObstaclesAtPosition = ignoreObstaclesAtPosition;
            _run = run;
            _savePosToServer = savePosToServer;
        }

        public override bool execute(AiContext newContext)
        {
            if (_currentWayPoint == null)
                return true;

            if (_wayPointReached)
                return true;

            if (!character.isMoving || !DistanceHelper.closePositions(_currentWayPoint, character.destination))
            {
                //Log.i("AiTaskGoToPosition->execute");
                character.speed = _run ? character.runSpeed : character.walkSpeed;
                character.moveToPosition((int)_currentWayPoint.x, (int)_currentWayPoint.y, onWayPoint, _ignoreObstaclesAtPosition);
            }

            return false;
        }

        private void onWayPoint(bool success, int x, int y)
        {
            //Log.i("AiTaskGoToPosition->onComplete:" + success);
            _wayPointReached = true;

            if (_savePosToServer && success && GameContext.world.map.canPlaceObject(character, (int)x, (int)y))
            {
                GameActionProcessor.instance.actionsQueue.enqueueActionWithObject(Command.MOVE, character.getMoveAction(character), character);
            }
        }
    }
}
