//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Platform - Platforms are either solid, or one-way. The engine only
//			   recognises collisions with solid platforms, and the kit will
//			   attempt to register one-way collisions through script. When a
//			   platform is created, it will attempt to find a "surfaceImage".
//			   This tells the kit what the surface of the platform looks like,
//			   and how an actor should react upon contact.
//-----------------------------------------------------------------------------

if (!isObject(PlatformBehavior))
{
	%template = new BehaviorTemplate(PlatformBehavior);

	%template.friendlyName = "Platform Object";
	%template.behaviorType = "Platform";
	%template.description  = "Platform object";

	%template.addBehaviorField( OneWay, 			"One-Way Platform",	BOOL,	false);
	
	%template.addBehaviorField( SurfaceFriction,	"Surface Friction",	FLOAT,	1.0);
	%template.addBehaviorField( SurfaceForce,		"Surface Force",	FLOAT,	0.0);
}

function PlatformBehavior::onAddToScene(%this, %scenegraph)
{
	// Find the collision details of the platform
	getCollisionMask(%this);
	%this.getSurfaceImage();
	
	// Record the initial position
	%this.Owner.InitialPosition = %this.Owner.Position;
	
	// Ensure the platform doesn't activly collide with anything
	%this.Owner.setCollisionActive(0, 1);
	%this.Owner.setCollisionPhysics(0, 0);
	%this.Owner.setCollisionCallback(%this.OneWay);
	%this.Owner.WorldLimitCallback = true;
	
	// Record the platform details
	%this.Owner.SurfaceFriction = %this.SurfaceFriction;
	%this.Owner.SurfaceForce	= %this.SurfaceForce;
	%this.Owner.OneWay 			= %this.OneWay;
	
	// This SimSet holds the id's of the actors currently on this platform
	%this.ActorsHolding = new SimSet();
	
	// Set the object type
	if (%this.OneWay)
		%this.Owner.setObjectType("OneWayPlatform");
	else
		%this.Owner.setObjectType("SolidPlatform");
}

function PlatformBehavior::getSurfaceImage(%this)
{
	%imageCount = 0;
	
	// Get some information about the object
	%reffSize	  = mVectorMultiply(%this.Owner.Size, 1 / 2);
	%reffPosition = %this.Owner.Position;
	%reffRotation = mRotationMatrix(%this.Owner.Rotation);
	%reffFlipMod  = 2 * !%this.Owner.FlipX - 1 SPC 2 * !%this.Owner.FlipY - 1;
	
	%polyList  = %this.Owner.getCollisionPoly();
	%polyCount = %this.Owner.getCollisionPolyCount();
	
	// Loop through each collision point between the min and max values
	%j = -1;
	for (%i = %this.Min.X; %j != %this.Max.X; %i++)
	{
		// Ensure the points wrap around properly
		if (%i == %polyCount)
			%i = 0;
			
		%j = (%i + 1 == %polyCount) ? 0 : %i + 1;
		
		// Get the two points
		%pA = getWords(%polyList, 2 * %i, 2 * %i + 1);
		%pB = getWords(%polyList, 2 * %j, 2 * %j + 1);
		
		// Adjust them for flips
		%pA = mVectorMultiply(%pA, %reffFlipMod);
		%pB = mVectorMultiply(%pB, %reffFlipMod);
		
		// Ensure the we work left to right
		if (%pA.X > %pB.X)
		{
			%pT = %pA;
			%pA = %pB;
			%pB = %pT;
		}
		
		// Scale by size
		%pA = mVectorMultiply(%pA, %reffSize);
		%pB = mVectorMultiply(%pB, %reffSize);
		
		// Rotate the points
		%pA = mMatrixMultiply(%reffRotation, %pA);
		%pB = mMatrixMultiply(%reffRotation, %pB);
		
		// Add the position
		%pA = t2dVectorAdd(%reffPosition, %pA);
		%pB = t2dVectorAdd(%reffPosition, %pB);
		
		// Find the normal between the points
		%dP = VectorSub(%pB, %pA);
		%pN = VectorNormalize(%dP.Y SPC -%dP.X);
		
		// Record the information about the image
		%surfaceImage = new ScriptObject();
		
		%surfaceImage.PointA = %pA;
		%surfaceImage.PointB = %pB;		
		%surfaceImage.Normal = %pN;
		
		// Store and increment
		%this.Owner.SurfaceImage[%imageCount] = %surfaceImage;
		%imageCount++;
	}
	
	// Record the number of surface images
	%this.Owner.SurfaceImageCount = %imageCount;
}

function PlatformBehavior::actorLanded(%this, %actor)
{
	// Record that we're holding an actor
	%this.ActorsHolding.add(%actor);
	
	// Notify other behaviors the actor has landed
	%behaviorCount = %this.Owner.getBehaviorCount();
	for (%i = 0; %i < %behaviorCount; %i++)
	{
		// Get the ith behavior
		%behavior = %this.Owner.getBehaviorByIndex(%i);
		
		// Skip this one
		if (%behavior == %this)
			continue;
		
		// Notify that the actor has landed
		if (%behavior.isMethod("actorLanded"))
			%behavior.actorLanded(%actor);
	}
}

function PlatformBehavior::actorLeft(%this, %actor)
{
	// Record that we're no longer holding an actor
	%this.ActorsHolding.remove(%actor);
	
	// Notify other behaviors the actor has left this platform
	%behaviorCount = %this.Owner.getBehaviorCount();
	for (%i = 0; %i < %behaviorCount; %i++)
	{
		// Get the ith behavior
		%behavior = %this.Owner.getBehaviorByIndex(%i);
		
		// Skip this one
		if (%behavior == %this)
			continue;
		
		// Notify that the actor has landed
		if (%behavior.isMethod("actorLeft"))
			%behavior.actorLeft(%actor);
	}
}

function PlatformBehavior::onWorldLimit(%this, %limitmode, %limit)
{
	// Update inherited velocities for each of the actors on this platform
	%actorCount = %this.ActorsHolding.getCount();
	for (%i = 0; %i < %actorCount; %i++)
	{
		%actor = %this.ActorsHolding.getObject(%i);
		
		// Update the inherited velocity
		%actor.InheritedVelocity = mVectorMultiply(%this.Owner.LinearVelocity, -1);
	}
}