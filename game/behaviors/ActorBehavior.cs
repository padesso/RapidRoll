//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Actor - In the starter kit, an actor is any character that interacts with
//			the world as a normal person would. They are affected by gravity,
//			inherit velocity from objects, jump, fall, pick up objects, etc.
//			Typically, both players AND enemies are actors, as they usually
//			interact with the world in the same fashion. The part that
//			makes them unique is the Controller. If you think of an Actor as a
//			body without thought (not real-life actors, just these ones ;),
//			then a Controller is the brain.
//-----------------------------------------------------------------------------

if (!isObject(ActorBehavior))
{
	%template = new BehaviorTemplate(ActorBehavior);
	
	%template.friendlyName	= "Actor Object";
	%template.behaviorType	= "Actor";
	%template.description	= "Handles actor movement and physics";
	
	%template.addBehaviorField( Gravity,				"Gravitic force applied to actor",					FLOAT,	3.5);
	%template.addBehaviorField( MaxMoveSpeed,			"Maximum running speed",							FLOAT,	35);
	%template.addBehaviorField( GroundAccel,			"Movement acceleration while running",				FLOAT,	3.0);
	%template.addBehaviorField( GroundDecel,			"Movement deceleration while running",				FLOAT,	5.0);
	%template.addBehaviorField( AirAccel,				"Movement acceleration while airborn",				FLOAT,	2.0);
	%template.addBehaviorField( AirDecel,				"Movement deceleration while ariborn",				FLOAT,	0.5);
	%template.addBehaviorField( JumpForce,				"Height the actor can jump",						FLOAT,	50);
	%template.addBehaviorField( AllowJumpDown,			"May pass downwards through one-way platforms",		BOOL,	true);
	
	%template.addBehaviorField( AllowBounceJump,		"May bounce jump up trampoline platforms",			BOOL,	true);
	%template.addBehaviorField( BounceJumpTimeOut,		"Maximum time before/after landing to bounce jump",	FLOAT,	0.05);
	
	%template.addBehaviorField( AllowGlide,				"Limit the actor's fall speed",						BOOL,	true);
	%template.addBehaviorField( GlideMaxFallSpeed,		"Maximum fall speed while gliding",					FLOAT,	10.0);
	%template.addBehaviorField( GlideTimeOut,			"Maximum glide time",								FLOAT,	2.0);
	
	%template.addBehaviorField( ClimbUpSpeed,			"Movement speed while climbing up", 				FLOAT,	25);
	%template.addBehaviorField( ClimbDownSpeed,			"Movement speed while climbing down", 				FLOAT,	45);
	%template.addBehaviorField( ClimbTimeOut,			"Time before actor can attach to another ladder", 	FLOAT,	1.0);
	%template.addBehaviorField( ClimbJumpCoefficient,	"Coefficient of jump force while on a ladder",		FLOAT,	0.5);
	
	%template.addBehaviorField( GroundCheckThreshold,	"Y-Distance to check grounding",					FLOAT,	0.3);
	%template.addBehaviorField( GroundYBuffer,			"Small buffer between feet and ground",				FLOAT,	0.05);
	%template.addBehaviorField( LadderAttachThreshold,	"Minimum x-distance to attach to a ladder",		 	FLOAT,	1.0);
	%template.addBehaviorField( MaxGroundNormalY,		"Maximum size of a sloped platform",				FLOAT,	-0.1);
	
	%template.addBehaviorField( MaxHealth,				"Maximum health",									FLOAT,	100);
	%template.addBehaviorField( Armor,					"Damage is scaled by the armor coefficient",		FLOAT,	0.0);
	%template.addBehaviorField( AllowRespawn,			"Allow this actor to respawn",						BOOL, 	true);
	%template.addBehaviorField( HideOnDeath,			"Hide after the death animation has played",		BOOL, 	false);
	%template.addBehaviorField( Lives,					"Number of lives this actor starts off with",		INT,	3);
	%template.addBehaviorField( SpawnTimeOut,			"Minimum time between death and respawn",			FLOAT,	3.0);
	%template.addBehaviorField( AttackTimeOut,			"Minimum time between attacks",						FLOAT,	0.40);
	%template.addBehaviorField( DamageTimeOut,			"Minimum time between recursive damage",			FLOAT,	0.75);
}

/// Set up the initial properties of the actor object
function ActorBehavior::onAddToScene(%this, %scenegraph)
{
	// Make sure there is a controller attached
	if (!%this.Owner.getBehavior(ControllerBehavior) && !%this.Owner.getBehavior(AIControllerBehavior))
		warn("Actor has no controller");
	
	// This is stored so other functions use the parameters here
	%this.Owner.ActorBehavior = %this;
	
	// Grab some details about the collision mask.
	// Note: It is probably best to keep your collision poly as symmetric as
	// 		 possible. This will prevent quite a few errors that arise when your
	//		 actor turns on a slope.
	getCollisionMask(%this);
	%this.MaskMax.Y += %this.GroundYBuffer;
	
	// Set up some of the basics we need down the track
	%this.Owner.setCollisionActive(1,1);
	%this.Owner.setCollisionPhysics(1,0);
	%this.Owner.setCollisionCallback(1);
	%this.Owner.enableUpdateCallback();
	
	// If an object type has not been specified, then just use default settings
	if (%this.Owner.ObjectType $= "")
	{
		%this.Owner.setObjectType("ActorObject");
		%this.Owner.setCollidesWith("PlatformObject ActorObject ActorTrigger");
	}
	
	// Inital state the actor will be in
	%this.State				= "inAir";
	%this.IsGliding			= false;
	%this.CanGlide			= %this.AllowGlide;
	%this.IsClimbing		= false;

	// We want to be alive!
	%this.Alive				= true;
	%this.Health			= %this.MaxHealth;
	
	// If the actor is killed, they will respawn in the location they originate
	// from, unless a Check Point is used.
	%this.RespawnPosition	= %this.Owner.getPosition();
	
	// These are used to store raw speeds and any movement vectors inherited
	// from others
	%this.MoveSpeed 		= 0.0 SPC 0.0;
	%this.InheritedVelocity = 0.0 SPC 0.0;
	
	// 
	%this.CorrectionVelocity = 0.0 SPC 0.0;
	
	// Sim set to store inventory items
	%this.InventoryList	= new SimSet()
	{
		CanSaveDynamicFields = true;
		
		MaxItems = 10;
	};
	
	// Script object that stores collision information
	%this.GroundCollision = new ScriptObject();
	
	// Notify the object this behavior belongs to that we've just spawned for
	// the first time
	if (%this.Owner.isMethod("onSpawn"))
		%this.Owner.onSpawn();
}

/// Every tick, the information about the actor's state must be updated. This
/// function establishes whether or not the actor is on the ground, then updates
/// its physics and animation settings accordingly.
function ActorBehavior::onUpdate(%this)
{
	// Updates the ground information each frame
	%this.updateOnGround();
	
	// Then update the actor's movement
	%this.updatePhysics();
	
	// Update any animations that need to be processed
	if (%this.Owner.AnimationManager)
		%this.Owner.AnimationManager.updateAnimation();
}

/// Determines the actor's state. If the state has not changed, then it will
/// return NULL and not update the current state. The current state can be
/// accessed through the %actorObject.State field.
function ActorBehavior::getState(%this)
{
	// Are we dead?
	if (!%this.Alive)
		return "isDead";
	
	if (%this.State $= "onGround")
	{
		// Climbing
		if (%this.Climbing)
			return "onLadder";
			
		// In the air
		if (!%this.onGround)
			return "inAir";
	}
	
	if (%this.State $= "inAir")
	{
		// Climbing
		if (%this.Climbing)
			return "onLadder";
		
		// On the ground
		if (%this.onGround)
			return "onGround";
	}
	
	if (%this.State $= "onLadder")
	{
		if (!%this.Climbing)
		{
			// On the ground
			if (%this.onGround)
				return "onGround";
			
			// We must be in the air otherwise
			return "inAir";
		}
	}
	
	// No change!
	return NULL;
}

/// Calls the appropriate functions to update the physics based on the state
/// that the actor is in. The reason this has been set up this way is to allow
/// you to easily create your own states and have different physics rules
/// applied to your actor based on that state.
function ActorBehavior::updatePhysics(%this)
{
	// See if the actor is trying to climb
	%this.updateClimbing();
	
	// Update the current state of the actor
	%newState = %this.getState();
	if (%newState !$= NULL)
		%this.State = %newState;
	
	// Check to see if we want to call overriding physics updates by our parent object
	if (%this.Owner.isMethod("update" @ %this.State @ "Physics"))
	{
		eval (%this.Owner @ ".update" @ %this.State @ "Physics();");
		return;
	}
	
	// Call the normal physics functions instead
	eval (%this @ ".update" @ %this.State @ "Physics();");
}

/// Determines whether the actor is climbing or not. It will only attach the
/// actor to the ladder when it is close enough to the center of the ladder
/// object, also ensuring that we are below the top of the ladder.
function ActorBehavior::updateClimbing(%this)
{
	// If we are climbing, but there is no ladder object, stop climbing
	if (%this.Climbing && !isObject(%this.LadderObject))
		%this.Climbing = false;
	
	// If we are climbing or there is no ladder object, just return
	if (!isObject(%this.LadderObject) || %this.Climbing)
		return;
	
	// Some information about the actor and the ladder
	%actorMin  = %this.Owner.Position.Y + %this.MaskMin.Y;
	%actorMax  = %this.Owner.Position.Y + %this.MaskMax.Y;
	%ladderMin = %this.LadderObject.Position.Y - (%this.LadderObject.Size.Y / 2);
	
	// Basically, we have to be close enough to the middle of the ladder, we
	// cannot climb higher than the top of the ladder and we want to prevent the
	// actor from reattaching to a ladder too early.
	if (mAbs(%this.LadderObject.Position.X - %this.Owner.Position.X) < %this.LadderAttachThreshold
		&& ((%this.Owner.Controller.Direction.Y < 0 && %actorMax > %ladderMin) || (%this.Owner.Controller.Direction.Y > 0 && (!%this.onGround || %this.GroundObject.OneWay)))
		&& (getSceneTime() - %this.ClimbDetatchTime > %this.ClimbTimeOut))
	{
		// If we're above the top of the ladder, clamp our position
		if (%actorMin < %ladderMin)
			%this.Owner.setPositionY(%ladderMin - %this.MaskMin.Y);
		
		// Flag that we are climbing
		%this.Climbing = true;
		
		// If we have any inherited velocity, lose it
		%this.InheritedVelocity = 0 SPC 0;
		
		// Attach ourselves to the middle of the ladder
		%this.Owner.setPositionX(%this.LadderObject.Position.X);
	}
}

/// Ground detection function determines if the actor is on the ground and then
/// finds the details of the object and the collision accordingly.
function ActorBehavior::updateOnGround(%this)
{
	// Don't bother checking for ground object if we're dead
	if (!%this.Alive)
		return;
	
	%groundObject = NULL;
	%pointMin 	  = NULL;
	%pointMax 	  = NULL;
	
	// This will find the list of potential ground objects
	%pickGround = %this.pickGround();
	
	// Loop through all potential ground objects
	%count = getWordCount(%pickGround);
	for (%i=0; %i<%count; %i++)
	{
		// Use this object
		%cObject = getWord(%pickGround, %i);
		
		// Skip non-platforms and ones that can't take the heat
		if (!%cObject.getBehavior(PlatformBehavior) || !%cObject.getCollisionActiveReceive())
			continue;
		
		// If it is a one way platform, and it is flagged to be skipped, skip it!
		if (%cObject.OneWay && (%this.JumpDownPlatform == %cObject || %this.Climbing))
			continue;
		
		// Grab the collision information about the object
		if (!%this.testGroundPoly(%cObject))
			continue;
			
		// Contact position and normal
		%cPosition = %this.GroundCollision.Position;
		%cNormal   = %this.GroundCollision.Normal;
		
		// If the slope is too great, then treat it as a wall
		if (%cNormal.Y > %this.MaxGroundNormalY)
			continue;
			
		// Grab the relative velocity
		%Velocity = %this.Owner.LinearVelocity;
		%Velocity = VectorSub(%Velocity, %cObject.LinearVelocity);
		
		// Normalize it
		if (%Velocity.X != 0 && %Velocity.Y != 0)
			%Velocity = VectorNormalize(%Velocity);
		
		// Grab the dot product
		%dot = VectorDot(%Velocity, %cNormal);
		
		// Ignore platforms that we are moving away from
		if ((!%this.onGround || %this.GroundObject != %cObject) && %dot > 0)
			continue;
		
		// A small mod which helps us move appropriately through sloped one-way platforms
		if (%this.onGround)
			%velMod = mAbs(%this.Owner.LinearVelocity.X) * $Tick;
		else
			%velMod = mAbs(%this.Owner.LinearVelocity.Y) * $Tick;
		
		// Make sure that the collision position of the one-way platform is appropriate
		if (%cObject.OneWay &&
			%this.Owner.Position.Y + %this.MaskMax.Y - 2 * %this.GroundCheckThreshold - %velMod > %cPosition.Y)
			continue;
		
		// If we've reached this point, we should have a ground object to use!
		if (%groundObject $= NULL || %cPosition.Y < %pointMin)
		{
			// Grab the minimum point
			if (%pointMin $= NULL || %cPosition.Y < %pointMin)
				%pointMin = %cPosition.Y;
			
			// Record collision information
			%groundObject = %cObject;
			%groundPoint  = %cPosition;
			%groundNormal = %cNormal;
		}
		
		if (%pointMin $= NULL || %cPosition.Y < %pointMin)
			%pointMin = %cPosition.Y;
	}
	
	// Notify the old platform we've left it
	if (%this.GroundObject && %groundObject != %this.GroundObject)
	{
		%oldPlatform = %this.GroundObject.getBehavior(PlatformBehavior);
		%oldPlatform.actorLeft(%this);
	}
	
	// If there was no object, just clear the details and exit
	if (%groundObject $= NULL)
	{		
		%this.PreviouslyOnGround   = %this.onGround;
		%this.onGround			   = false;
		
		%this.PreviousGroundObject = %this.GroundObject;
		%this.GroundObject		   = NULL;
		
		return;
	}
	
	// Update it now, just in case one of the platforms we're on wants to change this value
	%this.onGround = true;
	
	// If we've changed ground objects, make sure we stop climbing
	if (%groundObject != %this.GroundObject)
	{
		// Stop climbing
		%this.Climbing = false;
		
		// Remember the last platform we landed on
		%this.LastLanded = %groundObject;
		
		// Notify the new platform we've entered it
		%newPlatform = %groundObject.getBehavior(PlatformBehavior);
		%newPlatform.actorLanded(%this);
	}
	
	// If there was no ground object previously, then notify the owner that we've just landed!
	if (%this.GroundObject $= NULL)
	{
		// Note that we aren't gliding but we can glide again
		%this.IsGliding	= false;
		%this.CanGlide  = %this.AllowGlide;
		%this.GlideTime = 0;
	
		%this.JumpDownPlatform = NULL;
		
		if (%this.Owner.isMethod("onLand"))
			%this.Owner.onLand();
	}
	
	if (%this.GroundObject $= NULL || !mVectorsEqual(%this.GroundNormal, %groundNormal))
	{
		// Make sure that the correct contact point is used!
		if (%groundPoint.Y > %pointMin)
		{
			%dy = %pointMin - %groundPoint.Y;
			
			%dx = 0;
			if (%cNormal.X != 0)
				%dx = %dy * (%cNormal.Y / %cNormal.X);
			
			if (%cNormal.X < 0)
				%dx *= -1;
			
			%groundPoint = VectorAdd(%groundPoint, %dx SPC %dy);
		}
		
		// Our new position
		%newPosition = %this.Owner.Position.X SPC %groundPoint.Y - %this.MaskMax.Y;
		
		// The correction velocity is used in place of setting the position of
		// the actor manually. We find the position the actor should be in at
		// this point, but then find the velocity to make him at that point in
		// the next frame. When physics are updated, this velocity is applied
		// for one frame only. This ensures that a smooth transition to the
		// correct position for the actor is seen, and not a jitter in sight!
		%this.CorrectionVelocity = mVectorMultiply(t2dVectorSub(%newPosition, %this.Owner.Position), 1 / $Tick);
	}
	
	// Note where we are now
	%this.PreviousPosition = %this.Owner.Position;
	
	// Update the ground object's details
	%this.GroundObject = %groundObject;
	%this.GroundNormal = %groundNormal;
	%this.GroundTime   = getSceneTime();
}

/// Construct a rectangle which lies just along the feet of the actor then find and return all of the objects within that rectangle.
function ActorBehavior::pickGround(%this)
{
	// Initialise our vectors (this prevents errors later
	%rectMin  = "0 0";
	%rectMax  = "0 0";
	%rectSize = "0 0";
	
	// Get our position
	%ourPosition	= %this.Owner.Position;	
	%ourPosition.X += (2 * !%this.Owner.FlipX - 1) * (mAbs(%this.MaskMax.X) - mAbs(%this.MaskMin.X)) / 2;
	%ourVelocity	= %this.Owner.LinearVelocity;
	
	// The minimum point is at our feet
	%rectMin.X	 = %ourPosition.X - (%this.MaskMax.X - %this.MaskMin.X) / 2;
	%rectMin.Y	 = %ourPosition.Y + %this.MaskMax.Y;
	
	// Size the rectangle
	%rectSize.X	 = %this.MaskMax.X - %this.MaskMin.X;
	%rectSize.Y	 = 4 * %this.GroundCheckThreshold + mAbs(%ourVelocity.Y * $Tick);
	
	// If we're on the ground, resize it a little bit so that we can detect ground objects which may be sloped and just a tad higher than our current y-position. We don't really need to do this when we're in the air.
	if (%this.onGround)
	{
		// Increase the size based on our velocity so we can detect potential collisions
		%vx = mAbs(%ourVelocity.X) * $Tick;
		
		if (%this.Owner.FlipX)
			%rectMin.X  -= %vx;
		
		%rectSize.X += %vx;
		
		// Move up our minimum point and increase the size
		%rectMin.Y  -= %this.GroundCheckThreshold;
		%rectSize.Y += %this.GroundCheckThreshold;
	}
	
	// The lower right point is the min-point + the size
	%rectMax = (%rectMin.X + %rectSize.X) SPC (%rectMin.Y + %rectSize.Y);
	
	// Return the list of potential objects
	return %this.Owner.SceneGraph.pickRect(%rectMin, %rectMax, -1, -1, false, %this.Owner);
}

/// Determines the collision details of the actor with a potential ground object
function ActorBehavior::testGroundPoly(%this, %cObject)
{
	// Initialise the min and max vectors
	%srcMin = %srcMax = "0 0";
	
	// Find the bounds of the actor, used to check platform details
	%srcPosition    = %this.Owner.Position;
	%srcPosition.X += (2 * !%this.Owner.FlipX - 1) * (mAbs(%this.MaskMax.X) - mAbs(%this.MaskMin.X)) / 2;
	%srcWidth		= %this.MaskMax.X - %this.MaskMin.X;
	
	%srcMin.X		= %srcPosition.X - %srcWidth / 2;
	%srcMax.X		= %srcPosition.X + %srcWidth / 2;
	%srcMax.Y		= %srcPosition.Y + %this.MaskMax.Y - 4 * %this.GroundCheckThreshold;
	
	// Find the min and max x-axis values for the current object
	%dstMin = %cObject.SurfaceImage[0].PointA.X;
	%dstMax	= %cObject.SurfaceImage[%cObject.SurfaceImageCount - 1].PointB.X;
	
	// Grab the distance it may have moved since being added
	%dstOffset = VectorSub(%cObject.Position, %cObject.InitialPosition);
	
	%cCount = 0;
	for (%i = 0; %i < %cObject.SurfaceImageCount; %i++)
	{
		// Grab the poly details
		%pA = %cObject.SurfaceImage[%i].PointA;
		%pB = %cObject.SurfaceImage[%i].PointB;
		%pN = %cObject.SurfaceImage[%i].Normal;
		
		// Add the offset
		%pA = t2dVectorAdd(%pA, %dstOffset);
		%pB = t2dVectorAdd(%pB, %dstOffset);
		
		// If it is too far above our feet, then we don't want to check it
		if (%pA.Y < %srcMax.Y && %pB.Y < %srcMax.Y)
			continue;
		
		// Make sure that we're in the x-axis range
		if (!mAxisOverlap(%srcMin.X, %pA.X, %pB.X) && !mAxisOverlap(%srcMax.X, %pA.X, %pB.X)
			&& !mAxisOverlap(%pA.X, %srcMin.X, %srcMax.X) && !mAxisOverlap(%pB.X, %srcMin.X, %srcMax.X))
			continue;
		
		// Find the distance between each point
		%dX = %pB.X - %pA.X;
		%dY = %pB.Y - %pA.Y;
		
		// If our min and max extend beyond the points, clamp them
		%pMin = (%pA.X > %srcMin.X) ? %pA.X : %srcMin.X;
		%pMax = (%pB.X < %srcMax.X) ? %pB.X : %srcMax.X;
		
		// Find the overlap point
		if (%pN.X > 0)
			%pC = %pMin SPC %pA.Y + (%pMin - %pA.X) * (%dY / %dX);
		else
			%pC = %pMax SPC %pA.Y + (%pMax - %pA.X) * (%dY / %dX);
		
		// Make sure we don't extend too far on a slope
		if ((%pN.X > 0 && %pC.X <= %dstMin) || (%pN.X < 0 && %pC.X >= %dstMax))
			%pN = "0.00000 -1.00000";
		
		// Record the collision information
		%cPosition[%cCount] = %pC;
		%cNormal[%cCount]	= %pN;
		%cCount++;
	}
	
	// If there was nothing
	if (%cCount == 0)
		return false;
	
	// If there was only one point
	if (%cCount == 1)
	{
		%this.GroundCollision.Position = %cPosition[0];
		%this.GroundCollision.Normal   = %cNormal[0];
		
		return true;
	}
	
	// Loop through to find the minimum point
	%cMin = NULL;
	for (%i = 0; %i < %cCount; %i++)
	{
		%j = (%i + 1 == %cCount) ? 0 : %i + 1;
		
		// If any two lines meet such that they face different directions, then force both normals to be flat. This is so we walk nicely over peaks.
		if (%cNormal[%i].X * %cNormal[%j].X <= 0)
			%cNormal[%i] = %cNormal[%j] = "0.00000 -1.00000";
		
		// Find the minimum point
		if (%cMin $= NULL || %cPosition[%i].Y < %cMin)
		{
			%minIndex = %i;
			%cMin	  = %cPosition[%i].Y;
		}
	}
	
	// Record the information
	%this.GroundCollision.Position = %cPosition[%minIndex];
	%this.GroundCollision.Normal   = %cNormal[%minIndex];
	
	// Return there was a collision
	return true;
}

/// When there has been a collision registered, specific functions are called
/// based on the type of object that the actor collided with.
function ActorBehavior::onCollision(%ourObject, %theirObject, %ourRef, %theirRef, %time, %normal, %contacts, %points)
{
	// If we've hit a platform, do a few specific checks
	if (%theirObject.getBehavior(PlatformBehavior))
		%ourObject.resolvePlatformCollision(%theirObject, %normal);
	
	// If we've hit an enemy, do a few specific checks
	%aiController = %theirObject.getBehavior(AIControllerBehavior);
	if (%aiController)
		%ourObject.resolveEnemyCollision(%theirObject, %normal);
}

/// The actor has hit a platform. Some checks to determine whether or not X-axis movement needs to be stoped are made.
function ActorBehavior::resolvePlatformCollision(%ourObject, %theirObject, %normal)
{
	// Was it a ground surface?
	%groundSurface = (%normal.Y < %ourObject.MaxGroundNormalY);
	
	// If it isn't, then it must be a wall!
	if (!%groundSurface && mAbs(%normal.X) > 0)
	{
		// Kill all movement speed and inherited velocity
		%ourObject.MoveSpeed.X  	 = 0;
		%ourObject.InheritedVelocity = 0 SPC 0;
		
		// Tell our owner that there was a wall, if it needs to do anything
		if (%ourObject.Owner.isMethod("hitWall"))
			%ourObject.Owner.hitWall(%theirObject, %normal);
	}
}

/// Resolve collisions with enemy actors
function ActorBehavior::resolveEnemyCollision(%ourObject, %theirObject, %normal)
{
	// Make sure that we only resolve collisions once, and only with players!
	if (!%ourObject.Owner.getBehavior(ControllerBehavior))
		return;
	
	// In your game, you should set up your own methods of resolving enemy
	// collisions. This will more than likely be specific to your game, and the
	// enemies that reside within it. I have provided a small example of what
	// you might want to do when attempting to resolve these sorts of collisions.
	if (%ourObject.Owner.isMethod("resolveEnemyCollision"))
		%ourObject.Owner.resolveEnemyCollision(%theirObject, %normal);
}

/// Update actor physics while we are on ground
function ActorBehavior::updateOnGroundPhysics(%this)
{
	// Since we're on the ground, we want to do ground based physics mods. I
	// store the *raw* movement speed in %this.MoveSpeed because if I were to
	// just use the actual velocity values, the actor wouldn't be able to move
	// at all!
	
	// Modify the accel and decel speeds by the surface friction
	%MoveAccel = %this.GroundAccel * mPow(%this.GroundObject.SurfaceFriction, 1.5);
	%MoveDecel = %this.GroundDecel * mPow(%this.GroundObject.SurfaceFriction, 1.5);
	
	// Check to see if we need to modify our inherited velocity at all
	if (!mVectorsEqual(%this.InheritedVelocity, %this.GroundObject.LinearVelocity))
	{
		// Grab the ground friction and velocity
		%groundFriction = mPow(%this.GroundObject.SurfaceFriction, 2);
		%groundVelocity = %this.GroundObject.LinearVelocity;
		
		%dirMod = 2 * (%groundVelocity.X > %this.InheritedVelocity.X) - 1;
		
		// Dump it or inherit it gracefully
		if (%this.InheritedVelocity.X == 0)
			%this.InheritedVelocity.X = %groundVelocity.X;
		else
			%this.InheritedVelocity.X += %MoveDecel * %dirMod * %groundFriction;
		
		// Dump y-axis velocity
		%this.InheritedVelocity.Y  = %groundVelocity.Y;
		
		// If the difference is too small, just clamp it
		if (mAbs(%groundVelocity.X - %this.InheritedVelocity.X) < %this.GroundAccel * %this.GroundObject.SurfaceFriction)
			%this.InheritedVelocity.X = %groundVelocity.X;
	}

	// Check to see if we should move or not
	if (%this.Owner.Controller.Direction.X != 0)
	{
		// Increase our speed in the direction we are trying to go
		%this.MoveSpeed.X += %this.Owner.Controller.Direction.X * %MoveAccel;
	}
	else
	{
		// Since we're not trying to go anywhere, slow our speed down
		%dir = -1 + 2 * (%this.MoveSpeed.X > 0);

		if (mAbs(%this.MoveSpeed.X) > mAbs(%MoveDecel))
			%this.MoveSpeed.X -= %dir * %MoveDecel;
		else
			%this.MoveSpeed.X = 0;
	}
	
	// Make sure we dont move too quickly
	%this.MoveSpeed.X = mClamp(%this.MoveSpeed.X, -%this.MaxMoveSpeed, %this.MaxMoveSpeed);
	
	// These are the additional forces imposed upon an actor
	%externalForce = (%this.InheritedVelocity.X - %this.GroundObject.LinearVelocity.X)
						+ %this.GroundObject.SurfaceForce;
	
	// Find the appropriate move vector
	%gNormal = %this.GroundNormal;
	if (%gNormal.Y > 0)
		%mVector = mVectorMultiply(-%gNormal.Y SPC %gNormal.X, -%this.MoveSpeed.X + %externalForce);
	else
		%mVector = mVectorMultiply(-%gNormal.Y SPC %gNormal.X,  %this.MoveSpeed.X + %externalForce);
	
	// Add the ground object's velocity
	%MoveSpeed    = %mVector;	
	%MoveSpeed.X += %this.GroundObject.LinearVelocity.X;
	%MoveSpeed.Y += %this.GroundObject.LinearVelocity.Y;
	
	// Compensate for the correction velocity
	%cVelocity = %this.popCorrectionVelocity();
	%MoveSpeed = t2dVectorAdd(%MoveSpeed, %cVelocity);

	// Assign the new new speed
	%this.Owner.setLinearVelocity(%MoveSpeed);
}

/// Update actor physics while we are in the air
function ActorBehavior::updateInAirPhysics(%this)
{
	// Air accel and decel values
	%MoveAccel = %this.AirAccel;
	%MoveDecel = %this.AirDecel;
	
	// Slowly decrease inherited velocity
	if (!mVectorsEqual(%this.InheritedVelocity, "0 0"))
	{
		// Decrease it nicely
		%dirMod = 2 * (%this.InheritedVelocity.X > 0) - 1;
		
		// Decrease it twice as quickly while gliding
		if (%this.IsGliding)
			%dirMod *= 2;
		
		%this.InheritedVelocity.X -= %MoveDecel * %dirMod;
		
		// Kill all y-axis velocity
		%this.InheritedVelocity.Y = 0;
		
		// If the difference is too small, just clamp it
		if (mAbs(%this.InheritedVelocity.X) < %MoveDecel)
			%this.InheritedVelocity.X = 0;
	}
	
	// Same movement stuff as above
	if (%this.Owner.Controller.Direction.X != 0)
		%this.MoveSpeed.X += %this.Owner.Controller.Direction.X * %MoveAccel;
	else
	{
		%dir = -1 + 2 * (%this.MoveSpeed.X > 0);
		
		if (mAbs(%this.MoveSpeed.X) > mAbs(%MoveDecel))
			%this.MoveSpeed.X -= %dir * %MoveDecel;
		else
			%this.MoveSpeed.X = 0;
	}
	
	// Ensure we dont move too quickly
	%this.MoveSpeed.X = mClamp(%this.MoveSpeed.X, -%this.MaxMoveSpeed, %this.MaxMoveSpeed);
	
	// Set the x-axis velocity
	%this.Owner.setLinearVelocityX(%this.MoveSpeed.X);
	
	// Add any residual inherited velocity (so that jumping from moving
	// platforms, helps or impedes the jump distance)
	%MoveSpeed    = %this.Owner.LinearVelocity;
	%MoveSpeed.X += %this.InheritedVelocity.X;
	
	// If we haven't disabled gravity, increase it
	if (!%this.DisableGravity)
		%MoveSpeed.Y += %this.Gravity;
	
	// If the jump button is released, stop gliding
	if (%this.IsGliding && !%this.Owner.Controller.Jump)
		%this.IsGliding = false;
	
	// Update the glide status
	%glide = (%this.AllowGlide && %this.CanGlide && %this.Owner.Controller.Jump && %MoveSpeed.Y > 0);
	if (%glide)
	{
		if (!%this.IsGliding)
			%this.LastGlideTime = getSceneTime();
			
		%this.GlideTime     += getSceneTime() - %this.LastGlideTime;
		%this.LastGlideTime  = getSceneTime();
		
		%this.IsGliding = true;
		%MoveSpeed.Y    = mClamp(%MoveSpeed.Y, 0, %this.GlideMaxFallSpeed);
	}
	
	// Check if we should continue to glide
	if (%this.IsGliding && %this.GlideTime >= %this.GlideTimeOut)
	{
		%this.IsGliding				= false;
		%this.CanGlide				= false;
		%this.Owner.Controller.Jump = false;
	}
	
	// Set the new velocity
	%this.Owner.setLinearVelocity(%MoveSpeed);
}

/// Update actor physics while we are on a ladder
function ActorBehavior::updateOnLadderPhysics(%this)
{
	// Kill any kind of movement that we were using
	%this.MoveSpeed = 0 SPC 0;
	
	// Move up and down
	if (%this.Owner.Controller.Direction.Y > 0)
		%this.MoveSpeed.Y =  %this.ClimbDownSpeed;
	else if (%this.Owner.Controller.Direction.Y < 0)
	{
		%this.onGround   = false;
		%this.MoveSpeed.Y = -%this.ClimbUpSpeed;
	}
	else
		%this.MoveSpeed.Y = 0;
	
	// Make sure we're not trying to move above the top of the ladder
	%playerTop = %this.Owner.Position.Y + %this.MaskMin.Y + %this.MoveSpeed.Y * $Tick;
	%ladderTop = %this.LadderObject.Position.Y - %this.LadderObject.Size.Y / 2;
	if (%playerTop < %ladderTop)
		%this.MoveSpeed.Y  = 0;
	
	// Add the speed of the ladder, in case its moving
	%MoveSpeed    = %this.LadderObject.LinearVelocity;
	%MoveSpeed.Y += %this.MoveSpeed.Y;
	
	// Modify our velocity
	%this.Owner.setLinearVelocity(%MoveSpeed);
}

/// Update actor physics while we are dead!
function ActorBehavior::updateIsDeadPhysics(%this)
{
	// If we're spawning, make sure we cannot move
	if (%this.Spawning)
	{
		%this.Owner.LinearVelocity = 0 SPC 0;
		return;
	}
	
	// Check if a respawn event is sheduled
	if (!isEventPending(%this.RespawnEvent))
	{
		// Respawn or gameover stuff
		if (%this.Lives > 0)
		{
         %this.RespawnEvent = %this.schedule(%this.SpawnTimeOut * 1000, "respawn");	      
		}
		else
			gameOver();
	}
	
	// Kill x-axis velocity
	%Velocity	 = %this.Owner.LinearVelocity;
	%Velocity.X  = 0;
	
	// If we want gravity to apply, do it
	if (!%this.DisableGravity)
		%Velocity.Y += %this.Gravity;
	
	// Set velocity
	%this.Owner.setLinearVelocity(%Velocity);
}

/// Actor's jump function
function ActorBehavior::jumpUp(%this)
{
	// Check to see if we can jump bounce from a trampoline
	%bounceJump = (%this.AllowGlide && %this.LastLanded.getBehavior(TrampolineBehavior)
		&& %this.JumpTime - %this.GroundTime < %this.BounceJumpTimeOut);
	if (%bounceJump)
		%this.LastLanded.getBehavior(TrampolineBehavior).bounce(%this, %bounceJump);
	
	// Only perform a regular jump if we're on the ground, or climbing
	if (!%this.onGround && !%this.Climbing)
		return;
	
	// Handle the animations accordingly
	if (%this.Owner.AnimationManager)
	{
		if (%this.onGround)
		{
			if (%this.Owner.LinearVelocity.X != 0)
				%this.Owner.AnimationManager.setState("runJump");
			else
				%this.Owner.AnimationManager.setState("jump");
		}
		else if (%this.Climbing)
		{
			%this.Owner.AnimationManager.setState("climbJump");
		}
	}
	
	if (%this.onGround)
	{
		// Regular jump
		%this.MoveSpeed.Y = -%this.JumpForce;
		
		// Dump some of the surface force into the inherited velocity
		%this.InheritedVelocity.X += %this.GroundObject.SurfaceForce / 2;
	}
	else if (%this.Climbing)
	{
		// Jumping while on a ladder
		%this.MoveSpeed.Y = -%this.JumpForce * %this.ClimbJumpCoefficient;
		
		// This projects us in the desired direction
		if (%this.Owner.Controller.Direction.X != 0)
			%this.MoveSpeed.X = %this.MaxMoveSpeed * %this.ClimbJumpCoefficient * %this.Owner.Controller.Direction.X;
		
		// Make sure we notify that we've just detatched
		%this.ClimbDetatchTime = getSceneTime();
	}
	
	// Apply the new velocity
	%this.Owner.setLinearVelocity(%this.MoveSpeed);

	// Clear parameters
	%this.onGround = false;
	%this.Climbing = false;
}

/// If the actor is on a one way platform then we can fall through it if we are
/// holding down the DOWN key and then hit the jump button. If we are not on a
/// one way platform and this function is called, then a regular jump is performed.
function ActorBehavior::jumpDown(%this)
{
	// Only jump if we're on the ground
	if (!%this.onGround)
		return;
		
	// If we cant jump through it, jump up instead
	if (!%this.GroundObject.OneWay)
	{
		%this.jumpUp();
		return;
	}
	
	// Handle animations
	if (%this.Owner.AnimationManager)
	{
		if (%MoveSpeed.X != 0)
			%this.Owner.AnimationManager.setState("runFall");
		else
			%this.Owner.AnimationManager.setState("fall");
	}
	
	// Make sure we can fall through the current platform
	%this.onGround		   = false;
	%this.JumpDownPlatform = %this.GroundObject;
}

function ActorBehavior::healDamage(%this, %dAmount)
{
	// We have to be alive!
	if (!%this.Alive)
		return false;
	
	// Increase health by desired amount
	%this.Health += %dAmount;
	%this.Health = mClamp(%this.Health, 0, %this.MaxHealth);
	
	// Do something if needed
	if (%this.Owner.isMethod("onHeal"))
		%this.Owner.onHeal(%dAmount);
	
	return true;
}

/// This function should be called when the actor takes any form of damage.
/// This ensures that the correct animations are called and that everything is
/// updated properly. If you just deduct health from the field, then you will
/// not see any animations or updates to the GUI unless manually called.
function ActorBehavior::takeDamage(%this, %dAmount, %srcObject, %ignoreArmor, %ignoreTimeout)
{
	// We have to be alive!
	if (!%this.Alive)
		return false;
	
	// Skip it if we've taken damage recently
	if (!%ignoreTimeOut && (getSceneTime() - %this.LastDamageTime) < %this.DamageTimeout)
		return false;
	
	// If we're considering armor, modify the damage amount
	if (!%ignoreArmor)
		%dAmount *= %this.ArmorModifier;
	
	// Decrease health
	%startHealth  = %this.Health;
	%this.Health -= %dAmount;
	
	// Make sure it is within range
	%this.Health = mClamp(%this.Health, 0, %this.MaxHealth);
	
	// Either die or take damage
	if (%this.Health == 0)
		%this.die(%startHealth - %this.Health, %srcObject);
	else
		%this.tookDamage(%startHealth - %this.Health, %srcObject);
	
	// Knock us off the ladder
	if (%this.Climbing)
	{
		%this.Climbing		   = false;
		%this.ClimbDetatchTime = getSceneTime();
	}
	
	// Notify if needed
	if (%this.Owner.isMethod("onDamage"))
		%this.Owner.onDamage(%dAmount);
}

/// Make sure we can see that we took some damage
function ActorBehavior::tookDamage(%this, %dAmount, %srcObject)
{
	// Record the damage time
	%this.LastDamageTime = getSceneTime();
	
	// Handle animations
	if (%this.Owner.AnimationManager)
		%this.Owner.AnimationManager.setState("damage");
}

/// Called when the actor dies and updates various details
function ActorBehavior::die(%this, %dAmount, %srcObject)
{
	// Flag that we're dead and decrease lives
	%this.Alive = false;
	%this.Lives--;
	
	// Disable collisions
	%this.Owner.setCollisionActive(0, 0);
	
	// Handle animations
	if (%this.Owner.AnimationManager)
		%this.Owner.AnimationManager.setState("die");
	
	// Notify if needed
	if (%this.Owner.isMethod("onDeath"))
		%this.Owner.onDeath(%dAmount, %srcObject);
}

/// Respawns the actor at the current respawn position. This is by default,
/// the location that the actor starts off in the world, and at various check
/// point positions that you can add.
function ActorBehavior::respawn(%this)
{
	// Check if we are allowed to return from the dead
	if (!%this.AllowRespawn)
	{
		// If we don't want to respawn the object, then delete it
		%this.Owner.safeDelete();
		
		return;
	}
	
	// Clear the event
	%this.RespawnEvent = NULL;
	
	// Flag that we're spawning
	%this.Spawning = true;
	
	// Reset some of the physics stuff
	%this.Owner.Position		= %this.RespawnPosition;
	
	//TODO: Make sure the player can spawn safely
	//%tempSpawnPositionY = $lowestPlatform.getPositionY() - 25;
	//%tempSpawnPositionX = $lowestPlatform.getPositionX();
	//%this.Owner.Position	= %tempSpawnPositionX SPC %tempSpawnPositionY;
	
	%this.Owner.LinearVelocity	= 0 SPC 0;
	%this.MoveSpeed				= 0 SPC 0;
	%this.InheritedVelocity		= 0 SPC 0;
	%this.JumpDownPlatform		= NULL;
	%this.Owner.FlipX			= false;
	
	// Reset the health
	%this.Health = %this.MaxHealth;
	
	// Animate
	if (%this.Owner.AnimationManager)
		%this.Owner.AnimationManager.setState("spawn");
	
	// Notify if needed
	if (%this.Owner.isMethod("onRespawn"))
		%this.Owner.onRespawn();
}

/// Called by the animation manager to tell us we can move around again
function ActorBehavior::spawnFinished(%this)
{
	// We've stopped respawning, now we can do stuff
	%this.Alive	   = true;
	%this.Spawning = false;
	%this.onGround = false;
	%this.State	   = "inAir";
	%this.Owner.setCollisionActive(1, 1);
}

/// This returns the correction velocity and then clears it
function ActorBehavior::popCorrectionVelocity(%this)
{
	// Store, clear and return
	%cV = %this.CorrectionVelocity;
	%this.CorrectionVelocity = 0.0 SPC 0.0;
	
	return %cV;
}