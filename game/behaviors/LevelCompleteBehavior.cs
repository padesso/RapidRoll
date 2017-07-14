//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Level Complete - This must be attached to a pick-up behavior. It will
//					 trigger the game over sequence and stores a field for you
//					 to specify the next level to be loaded (though I haven't
//					 provided the scripts to load another level).
//-----------------------------------------------------------------------------

if (!isObject(LevelCompletePickupBehavior))
{
	%template = new BehaviorTemplate(LevelCompletePickupBehavior);
	
	%template.friendlyName	= "Level Complete Pick-up";
	%template.behaviorType	= "Collectable";
	%template.description	= "Complete the current level";
	
	%template.addBehaviorField( NextLevel,	"Loads this level when complete",	DEFAULT, "");
}

function LevelCompletePickupBehavior::confirmPickup(%this, %targetObject, %inventoryItem)
{
	if (!%targetObject.ActorBehavior)
		return false;
	
	// Ensure the player cannot respawn
	%targetObject.ActorBehavior.AllowRespawn = false;
	
	// Stop moving
	%targetObject.Controller.Direction	  = "0 0";
	%targetObject.ActorBehavior.MoveSpeed = "0 0";
	%targetObject.setLinearVelocityX(0);
	
	// Stop the controller
	moveMap.pop();
	
	// Run the level complete function
	levelComplete();
	
	// Load the next level
	loadNewLevel(%this.NextLevel, 5000);
		
	return true;
}