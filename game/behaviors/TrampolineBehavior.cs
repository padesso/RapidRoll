//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Trampoline - A type of platform that allows actors to bounce off the
//				 surface. It will project the actor in the direction that the
//				 platform is rotated. The jump modifier is applied when the
//				 actor jumps shortly after contact with the platform.
//-----------------------------------------------------------------------------

if (!isObject(TrampolineBehavior))
{
	%template = new BehaviorTemplate(TrampolineBehavior);

	%template.friendlyName = "Trampoline Platform";
	%template.behaviorType = "Platform";
	%template.description  = "Trampoline Platform object";

	%template.addBehaviorField( BounceForce,	"Bounce height",	FLOAT,	100);
	%template.addBehaviorField( JumpModifier,	"Jump modifier",	FLOAT,	1.5);
}

function TrampolineBehavior::onAddtoScene(%this, %scenegraph)
{
	// Skip if we're not a platform
	if (!%this.Owner.getBehavior(PlatformBehavior))
	{
		error("FallingPlatformBehavior must be added to a platform!");
		return;
	}
	
	if (!%this.Owner.getBehavior(PlatformBehavior).OneWay)
		%this.Owner.setCollisionCallback(1);
	
	%this.RotationMatrix = mRotationMatrix(%this.Owner.Rotation);
}

function TrampolineBehavior::actorLanded(%this, %actor)
{
	// See if we should be jump bouncing
	%bounceJump = (getSceneTime() - %actor.JumpTime < %actor.BounceJumpTimeOut);
	
	// Bounce the actor
	%this.bounce(%actor, %bounceJump);
}

function TrampolineBehavior::onCollision(%ourObject, %theirObject, %ourRef, %theirRef, %time, %normal, %contacts, %points)
{
	if (%theirObject.getBehavior(ActorBehavior))
		%ourObject.bounce(%theirObject.ActorBehavior, false);
}

function TrampolineBehavior::bounce(%this, %actor, %bounceJump)
{
	// Get the modifier
	%jumpMod = (%bounceJump) ? %this.JumpModifier : 1.0;
	
	// Find the appropriate movement vector
	%moveSpeed = %actor.MoveSpeed.X SPC -%this.BounceForce * %jumpMod;
	%moveSpeed = mMatrixMultiply(%this.RotationMatrix, %moveSpeed);
	
	// Dump the velocity as inherited
	%actor.InheritedVelocity = %moveSpeed;
	
	// Update the actor's velocity
	%actor.Owner.setLinearVelocity(%moveSpeed);
	
	// Make sure we're not on the ground anymore
	%actor.onGround = false;
	
	// Play the sound and animate
	if (%actor.Owner.AnimationManager.CurrentState !$= "jump")
	{
		playSound(MushroomBounceSound);
		%actor.Owner.AnimationManager.setState("jump");
	}
	
	// Animate the platform
	if (%this.Owner.isMemberOfClass(t2dAnimatedSprite))
		%this.Owner.playAnimation(%this.Owner.AnimationName);
}