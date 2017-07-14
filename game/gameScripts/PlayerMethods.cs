//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Player Methods - A player is an Actor with a Player controller. The methods
//					 in this file make the game know what to do with a player.
//					 Adding specific methods to object classes gives more
//					 variety and control to the game.
//-----------------------------------------------------------------------------

/// Mount the camera and update the gui
function PlayerClass::onAddToScene(%this, %scenegraph)
{
	// Set the parallax target to this object
	$ParallaxScrollTarget = %this;
	
	%this.mountCamera();
	
	%this.updateExtraLivesGui();
	%this.updateHealthGui();
}

/// Mount the camera if required
function PlayerClass::onLevelLoaded(%this)
{
	// If you add the player straight into the scene and not through a spawner,
	// then you need to wait until the level has been loaded to mount the
	// camera.
	%this.mountCamera();
}

/// Called when the player lands on a platform
function PlayerClass::onLand(%this)
{
	if (isObject(DragonLandSound))
		%this.AnimationManager.playSound(DragonLandSound);
}

/// Called when the player is healed
function PlayerClass::onHeal(%this, %hAmount)
{
	%this.updateHealthGui();
}

/// Called when the player is damaged
function PlayerClass::onDamage(%this, %dAmount)
{
	%this.updateHealthGui();
}

/// Called when a player dies
function PlayerClass::onDeath(%this, %dAmount, %srcObject)
{
	// Dismount the camera
	dismountCamera();
	
	// Bounce the player
	%this.setLinearVelocityY(-%this.ActorBehavior.JumpForce);
	
	// Update the gui
	%this.updateExtraLivesGui();
	%this.updateHealthGui();
}

/// Called when a player respawns
function PlayerClass::onRespawn(%this)
{
	// Load the checkpoint data
	loadCheckPoint();
	
	// Remount the camera
	%this.mountCamera();
	
	// Update the gui
	%this.updateHealthGui();
}

/// Called when a player collides with an enemy.
function PlayerClass::resolveEnemyCollision(%ourObject, %theirObject, %normal)
{
	// Resolve the collisions differently for different ai types
	switch$ (%theirObject.Controller.AIType)
	{
		case "Drill" : %ourObject.resolveDrillCollision(%theirObject, %normal);
	}
}

/// Called upon collision with a drill type enemy
function PlayerClass::resolveDrillCollision(%ourObject, %theirObject, %normal)
{
	// Get the contact angle
	%angle = mRadToDeg(mAtan(-%normal.X, %normal.Y));
	%angle = mAbs(%angle) % 360;
	
	// Check if we've hit the drill properly
	if ((%angle <= 40 || %angle >= 360 - 40)
		&& %ourObject.LinearVelocity.Y > %theirObject.LinearVelocity.Y
		&& %ourObject.Position.Y < %theirObject.Position.Y)
	{
		// Do damage to the drill and bounce the player
		%theirObject.ActorBehavior.takeDamage(%theirObject.ActorBehavior.Health, %ourObject, true, true);
		%ourObject.setLinearVelocityY(-%ourObject.ActorBehavior.JumpForce / 2);
	}
	else
	{
		// Do damage to the player
		%ourObject.ActorBehavior.takeDamage(10, %theirObject, true, false);
	}
}

/// Update the health interface
function PlayerClass::updateHealthGui(%this)
{
	%pepperCount = %this.ActorBehavior.Health / 10;
	for (%i = 0; %i < 10; %i++)
	{
		%ghostVisible = !(%i < %pepperCount);
		
		if (isObject("ghostPepper" @ 9 - %i))
			eval ("ghostPepper" @ 9 - %i @ ".Visible = %ghostVisible;");
		
		if (isObject("pepper" @ 9 - %i))
			eval ("pepper" @ 9 - %i @ ".Visible = !%ghostVisible;");
	}
}

/// Update the extra life interface
function PlayerClass::updateExtraLivesGui(%this)
{
	if (isObject(ExtraLivesCounter))
	{
		ExtraLivesCounter.Text = %this.ActorBehavior.Lives;
		
		if (%this.ActorBehavior.Lives < 10)
			ExtraLivesCounter.Text = 0 @ %this.ActorBehavior.Lives;
	}
}