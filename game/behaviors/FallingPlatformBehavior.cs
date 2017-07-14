//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Falling Platform - When an actor lands upon this platform, they will have
//					   a few moments (specified by the FallTimeOut field)
//					   before the platform falls from the sky.
//-----------------------------------------------------------------------------

if (!isObject(FallingPlatformBehavior))
{
	%template = new BehaviorTemplate(FallingPlatformBehavior);

	%template.friendlyName = "Falling Platform";
	%template.behaviorType = "Platform";
	%template.description  = "Falling Platform object";

	%template.addBehaviorField( AutoRecover, 	"Respawn, otherwise destroy",	BOOL,	true);
	
	%template.addBehaviorField( Gravity,		"Gravity applied when falling",	FLOAT,	4.0);
	%template.addBehaviorField( FallTimeOut,	"Time before falling",			FLOAT,	0.5);
	%template.addBehaviorField( RecoverTimeOut,	"Time before recovery",			FLOAT,	2.0);
	
	%template.addBehaviorField( FallAnimation,		"Animation played while falling",		OBJECT, "", t2dAnimationDatablock);
	%template.addBehaviorField( RecoverAnimation, 	"Animation played while recovering",	OBJECT, "", t2dAnimationDatablock);
}

function FallingPlatformBehavior::onAddtoScene(%this, %scenegraph)
{
	// Skip if we're not a platform
	if (!%this.Owner.getBehavior(PlatformBehavior))
	{
		error("FallingPlatformBehavior must be added to a platform!");
		return;
	}
	
	// Skip if we're not an animated sprite
	if (!%this.Owner.isMemberOfClass(t2dAnimatedSprite))
	{
		error("FallingPlatformBehavior must be added to an t2dAnimatedSprite");
		return;
	}
	
	// Make sure we don't update just yet
	%this.Owner.disableUpdateCallback();
	
	// Recovery position
	%this.RecoverPosition = %this.Owner.Position;
}

function FallingPlatformBehavior::onUpdate(%this)
{
	// Get current velocity
	%moveSpeed   = %this.Owner.getLinearVelocity();
	
	// Add gravity
	%moveSpeed.Y += %this.Gravity;
	
	// Apply force
	%this.Owner.setLinearVelocity(%moveSpeed);
	
	if (%this.Owner.getIsAnimationFinished())
	{
		// Hide the platform
		%this.Owner.Visible = false;
		
		if (%this.AutoRecover)
		{
			// Stop updating
			%this.Owner.disableUpdateCallback();
			
			// Schedule the recovery
			%this.schedule(%this.RecoverTimeOut * 1000, "startRecovery");
		}
		else
		{
			// Destroy the object instead
			%this.Owner.safeDelete();
		}
	}
}

function FallingPlatformBehavior::actorLanded(%this, %actor)
{
	// Schedule this platform to fall
	%this.schedule(%this.FallTimeOut * 1000, "startFall");
}

function FallingPlatformBehavior::startFall(%this)
{
	// Start updating
	%this.Owner.enableUpdateCallback();
	
	// Stop us colliding with things
	%this.Owner.setCollisionActiveReceive(0);
	
	// Set the falling animation
	%this.Owner.setAnimation(%this.FallAnimation);
}

function FallingPlatformBehavior::startRecovery(%this)
{
	// Make sure we're visible
	%this.Owner.Visible = true;
	
	// Make sure we can collide with things
	%this.Owner.setCollisionActiveReceive(1);
	
	// Kill velocity
	%this.Owner.setLinearVelocity("0.0 0.0");
	
	// Reposition the platform
	%this.Owner.setPosition(%this.RecoverPosition);
	
	// Set the recovery animation
	%this.Owner.setAnimation(%this.RecoverAnimation);
}