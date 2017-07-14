//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  AI Controller - This controller is a very basic method of controlling our
//					AI. The actor will only move in one direction until told
//					otherwise. This method is the basic implementation of any
//					game AI and provides a foundation for you to expand upon.
//-----------------------------------------------------------------------------

if (!isObject(AIControllerBehavior))
{
	%template = new BehaviorTemplate(AIControllerBehavior);
	
	%template.friendlyName	= "AI Controller";
	%template.behaviorType	= "Actor";
	%template.description	= "Movement";
	
	%template.addBehaviorField( AIType, "Type of AI", ENUM, "", "None" NL "Drill");
}

/// Set up the controller
function AIControllerBehavior::onAddToScene(%this, %scenegraph)
{
	// Check if this behavior isn't attached to an actor
	if (!%this.Owner.getBehavior(ActorBehavior))
		return;
	
	// Record the controller id
	%this.Owner.Controller = %this;
	
	// Initialise the direction
	%this.Direction = (2 * !%this.Owner.FlipX) - 1 SPC 0;
	
	// Set up the collision references
	if (%this.AIType !$= "None")
	{
		%this.Owner.setObjectType("EnemyObject");
		%this.Owner.setCollidesWith("SolidPlatform PlayerObject EnemyTrigger");
	}
}