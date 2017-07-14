//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Spawn Point - Spawn points spawn a target object. Objects are specified in
//				  script only and do not clone current scene objects. They will
//				  spawn automatically, but can be forced to spawn the target
//				  object. They may also despawn any spawned objects if the
//				  camera pans too far away from the spawn point and the object.
//-----------------------------------------------------------------------------

if (!isObject(SpawnPointBehavior))
{
	%template = new BehaviorTemplate(SpawnPointBehavior);
	
	%template.friendlyName	= "Spawn Point";
	%template.behaviorType	= "Miscellaneous";
	%template.description	= "Spawns target object when the camera gets within range";
	
	%template.addBehaviorField( TargetObject, 		"Spawn this object (uses datablocks)",	OBJECT,	"", t2dSceneObjectDatablock);
	%template.addBehaviorField( MinSpawnDistance,	"Minimum spawn distance",				FLOAT,	60);
	%template.addBehaviorField( MaxSpawnDistance,	"Maximum spawn distance",				FLOAT,	100);
	
	%template.addBehaviorField( AutoDespawn,	"Despawn object when they are far away",	BOOL,	true);
	%template.addBehaviorField( NumberToSpawn,	"Number of objects",						INT,	1);
	%template.addBehaviorField( SpawnInterval,  "Time between spawns (sec)",				FLOAT,	5);
	%template.addBehaviorField( SpawnOnKill,	"Spawn another object when one is killed",	BOOL,	false);
}

function SpawnPointBehavior::onAddToScene(%this, %scenegraph)
{
	// Set up the details if we have a target
	if (isObject(%this.TargetObject) && %this.NumberToSpawn > 0)
	{
		%this.Owner.setObjectType("SpawnPointObject");
		%this.Owner.setCollidesWith("None");
		
		%this.Owner.enableUpdateCallback();
		%this.NumberSpawned    = 0;
		%this.CheckPointRecord = 0;
		%this.LastSpawned      = NULL;
		
		// This records the id's of any spawned objects
		%this.SpawnedObjects = new SimSet();
	}
	
	// Hide this object
	%this.Owner.Visible = false;
}

function SpawnPointBehavior::onUpdate(%this)
{
	if (%this.NumberSpawned < %this.NumberToSpawn)
	{
		// Get the positions
		%cameraPosition  = sceneWindow2D.getCurrentCameraPosition();
		%spawnPoint		 = %this.Owner.Position;
		
		// Find the distance to the camera
		%distanceToCamera = VectorDist(%cameraPosition, %spawnPoint);
		
		// Try to spawn the target
		if (%distanceToCamera > %this.MinSpawnDistance && %distanceToCamera < %this.MaxSpawnDistance)
			%this.spawnTarget();
	}
	
	if (%this.AutoDespawn && %this.NumberSpawned > 0)
	{
		for (%i = 0; %i < %this.SpawnedObjects.getCount(); %i++)
		{
			// Find the object
			%spawnedObject = %this.SpawnedObjects.getObject(%i);
			
			// Get the camera position
			%cameraPosition  = sceneWindow2D.getCurrentCameraPosition();
			
			// Ensure that both the spawn point and object are in the same direction of the camera
			if ((%spawnedObject.Position.X - %cameraPosition.X < 0) != (%this.Owner.Position.X - %cameraPosition.X < 0))
				continue;
			
			// Find the distances
			%objectToCamera = VectorDist(%cameraPosition, %spawnedObject.Position);
			%pointToCamera  = VectorDist(%cameraPosition, %this.Owner.Position);
			
			// Despawn the target if it is far enough away
			if (%objectToCamera > %this.MaxSpawnDistance * 1.5
				&& %pointToCamera > %this.MaxSpawnDistance * 1.5)
			{
				%this.despawnTarget(%spawnedObject);
				%i -= 1;
			}
		}
	}
}

function SpawnPointBehavior::spawnTarget(%this, %forceSpawn)
{
	// Check if we should spawn or not
	if (!%forceSpawn && (%this.NumberSpawned != 0)
		&& (%this.NumberSpawned >= %this.NumberToSpawn
		|| (getSceneTime() - %this.SpawnTime < %this.SpawnInterval)
		|| %this.SpawnOnKill && isObject(%this.LastSpawned)))
		return;
	
	// Create the new object
	%newObject = %this.createTarget();
	
	// Store information
	%this.NumberSpawned += 1;
	%this.SpawnTime      = getSceneTime();
	%this.LastSpawned	 = %newObject;
	
	// Ensure the new object faces in the direction of the camera
	if (%newObject.Controller && %newObject.Controller.Direction.X != 0)
	{
		%cameraPosition 				  = sceneWindow2D.getCurrentCameraPosition();
		%newObject.Controller.Direction.X = 2 * (%cameraPosition.X > %newObject.Position.X) - 1;
	}
	
	// Add it to the list
	%this.SpawnedObjects.add(%newObject);
}

function SpawnPointBehavior::despawnTarget(%this, %target)
{
	// Remove it from the list
	%this.SpawnedObjects.remove(%target);
	
	// Remove it from the game
	%target.safeDelete();
	
	// Decrease spawn counter
	%this.NumberSpawned -= 1;
}

function SpawnPointBehavior::createTarget(%this)
{
	// Create the new object
	%newObject = new t2dAnimatedSprite()
	{
		Config     = %this.TargetObject;
		
		Scenegraph = %this.Owner.Scenegraph;
		Position   = %this.Owner.Position;
		
		SpawnPoint	   = %this;
	};
	
	// Return it's id
	return %newObject;
}