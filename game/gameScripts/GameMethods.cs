//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Game Methods - This file contains many of the global functions that are
//				   used throughout this kit.
//-----------------------------------------------------------------------------

//Globals
$platformSpawnMin = 1000;         //Min Time between platform spawns
$platformSpawnMax = 2000;        //Max Time between platform spawns
$fallingPlatformChance = 4;      //Chance of a spawned platform being a faller
$scrollSpeedInterval = 3500;       //How often the $scrollSpeed is increased (ms)
$scrollSpeedOffset = 2;          //How much the $scrollSpeed is increased (ms)
$winScore = 1000;               //Score required to win level
$maxScrollSpeed = 30;            //Clamps the scrollspeed

/// Sets up the foundations for the kit
function initialisePlatformerKit()
{
	// Average time between each frame
	$Tick = 0.032;

	// Loads the custom collision system
	exec ("./ObjectManager.cs");
	registerObjectType("SpawnPointObject");
	registerObjectType("PlatformObject OneWayPlatform");
	registerObjectType("PlatformObject SolidPlatform");
	registerObjectType("ActorObject PlayerObject");
	registerObjectType("ActorObject EnemyObject");
	registerObjectType("ActorTrigger PlayerTrigger");
	registerObjectType("ActorTrigger EnemyTrigger");
	
	// This handles sound information
	exec ("./SoundManager.cs");
}

/// Returns the scene time
function getSceneTime()
{
	return sceneWindow2D.getSceneGraph().getSceneTime();
}

/// Load any ingame features here
function t2dSceneGraph::onLevelLoaded(%this)
{
   //initialize some globals that must be reset at death
   $score = 0;                        //Players score, incremented by falling (not gliding)
   $scrollSpeed = 10;                //Initial scroll speed
   
	// Ensure that the channel volumes are reset
	%soundChannel = getSoundChannel();
	%soundVolume  = $SoundManager::Sound::Volume;
	
	%musicChannel = getMusicChannel();
	%musicVolume  = $SoundManager::Music::Volume;
	
	alxSetChannelVolume(%soundChannel, %soundVolume);
	alxSetChannelVolume(%musicChannel, %musicVolume);
	
	// Play the music
	if (isObject(OutdoorMusic))
		playMusic(OutdoorMusic);
	
	$platformList = new SimSet();
	
	updateScrollers();	
	startTimers();
}

function updateScrollers()
{  
   for(%i=0; %i < 7; %i++)
   { 
      %leftName = "leftWall"@%i;
      %leftName.setScrollX(-$scrollSpeed);
      
      %rightName = "rightWall"@%i;
      %rightName.setScrollX(-$scrollSpeed);
   }

   safePlatform.setLinearVelocityY(-$scrollSpeed);   
   
   //Loop through all platforms in the simset and set the updated scrollSpeed
   for(%j=0; %j < $platformList.getCount(); %j++)
   {
      $platformList.getObject(%j).setLinearVelocityY(-$scrollSpeed);
   }
}

/// Mounts the scene camera to the object
function t2dSceneObject::mountCamera(%this)
{
	if (sceneWindow2D.getIsCameraMounted())
		dismountCamera();
		
	sceneWindow2D.mount(%this, "0 0", 15, false);
}

/// Bug fix
function t2dSceneObject::onFrameChange(%this, %frame)
{
	// Without this empty function TGB will not call the behavior's "onFrameChange" function
}

/// Dismounts the scene camera
function dismountCamera(%this)
{
	if (!sceneWindow2D.getIsCameraMounted())
		return;
	
	sceneWindow2D.dismount();
}

function loadNewLevel(%levelFile, %delay)
{
	// Prevents a crash ;)
	if (%delay $= "") %delay = 100;
	
	// If you haven't included a directory then we'll use the default
	if (!isFile(%levelFile))
		%levelFile = "game/data/levels/" @ %levelFile;
	
	// Add on the extension if needed
	%fileExtn = ".t2d";
	if (getSubStr(%levelFile, strlen(%levelFile) - strlen(%fileExtn), strlen(%fileExtn)) !$= %fileExtn)
		%levelFile = %levelFile @ %fileExtn;
	
	// Load the file if possible
	if (isFile(%levelFile))
		sceneWindow2D.schedule(%delay, "loadLevel", %levelFile);
	else
		warn("Level not found: " @ %levelFile);
}

/// Displays the game over sequence
function gameOver()
{
	if (isObject(gameOverBanner))
	{
		// Dismount the camera
		dismountCamera();
		
		// Show the game over banner
		gameOverBanner.Position = sceneWindow2D.getCurrentCameraPosition();
		gameOverBanner.Visible  = true;
		
		// Stop the current music
		stopMusic();
	}
	
	restartLevel();
}

function restartLevel()
{     
   speedController.setTimerOff();
   platformSpawner.setTimerOff();   
   
   //Why does the spawn animation stutter?
   loadNewLevel("untitled.t2d", 2000);
}

/// Displays the level completed sequence
function levelComplete()
{
	if (isObject(congratulationsBanner))
	{
		// Dismount the camera
		dismountCamera();
		
		// Show the congratulations banner
		congratulationsBanner.Position = sceneWindow2D.getCurrentCameraPosition();
		congratulationsBanner.Visible  = true;
		
		if (isObject(LevelCompleteMusic))
		{
			// Stop any playing sounds and mute the sound effects channel
			alxStopAll();
			alxSetChannelVolume(getSoundChannel(), 0.0);
			
			// Play the level complete music
			playMusic(LevelCompleteMusic);
		}
	}
}

/// Obtains the min and max bounds of an object
function getCollisionMask(%cObject)
{
	// Get the rotation matrix
	%reffRotation = mRotationMatrix(%cObject.Owner.Rotation);
	
	// Grab all of the poly points
	%polylist  = %cObject.Owner.getCollisionPoly();
	%polyCount = %cObject.Owner.getCollisionPolyCount();

	// Initialise min and max vectors
	%Min = %Max = "0 0";

	// Check each point
	for(%i = 0; %i < %polyCount; %i++)
	{
		// Grab the point
		%vPoint[%i] = getWords(%polyList, 2 * %i, 2 * %i + 1);
		
		// Make sure we rotate the points
		%vPoint[%i] = mMatrixMultiply(%reffRotation, %vPoint[%i]);

		// Store max x-axis value
		if (%Max.X $= NULL || %vPoint[%i].X >= %vPoint[%Max.X].X)
		{
			// Make sure it is the top point
			if (%vPoint[%i].X == %vPoint[%Max.X].X)
			{
				if (%vPoint[%i].Y < %vPoint[%Max.X].Y)
					%Max.X = %i;
			}
			else
			{
				%Max.X = %i;
			}
		}
		
		// Store min x-axis value
		if (%Min.X $= NULL || %vPoint[%i].X <= %vPoint[%Min.X].X)
		{
			// Make sure it is the top point
			if (%vPoint[%i].X == %vPoint[%Min.X].X)
			{
				if (%vPoint[%i].Y < %vPoint[%Min.X].Y)
					%Min.X = %i;
			}
			else
			{
				%Min.X = %i;
			}
		}

		// Store max y-axis values
		if (%Max.Y $= NULL || %vPoint[%i].Y > %vPoint[%Max.Y].Y)
			%Max.Y = %i;
		
		// Store min y-axis values
		if (%Min.Y $= NULL || %vPoint[%i].Y < %vPoint[%Min.Y].Y)
			%Min.Y = %i;
	}
	
	// Size of the object
	%objectSize = %cObject.Owner.Size;
	
	// Size the masks properly
	%cObject.MaskMin.X = %vPoint[%Min.X].X * %objectSize.X / 2;
	%cObject.MaskMin.Y = %vPoint[%Min.Y].Y * %objectSize.Y / 2;
	
	%cObject.MaskMax.X = %vPoint[%Max.X].X * %objectSize.X / 2;
	%cObject.MaskMax.Y = %vPoint[%Max.Y].Y * %objectSize.Y / 2;
	
	// Record the ith points in the list
	%cObject.Min = %Min;
	%cObject.Max = %Max;
}

/// Check if word, w, in a string, s
function sInString(%w, %s)
{
	for (%i = 0; %i < getWordCount(%s); %i++)
		if (%w $= getWord(%s, %i))
			return true;
	
	return false;
}

/// %vA = 2d vector; %vB = int/2d vector
function mVectorMultiply(%vA, %vB)
{
	%x = (%vA.X * %vB.X);
	%y = (getWordCount(%vB) > 1) ? (%vA.Y * %vB.Y) : (%vA.Y * %vB.X);
	
	return %x SPC %y;
}

/// Ensures x isn't outside of the min and max values
function mClamp(%x, %min, %max)
{
	if (%x < %min)
		return %min;
	
	if (%x > %max)
		return %max;
		
	return %x;
}

/// See if two vectors are equal
function mVectorsEqual(%va, %vb)
{
	return (%va.x == %vb.x && %va.y == %vb.y);
}

/// See if a point is within a line
function mAxisOverlap(%x, %vA, %vB)
{
	if (%x >= %vA && %x <= %vB)
		return true;
	
	return false;
}

function mMin(%a, %b)
{
	if (%a < %b)
		return %a;
	
	return %b;
}

function mMax(%a, %b)
{
	if (%a > %b)
		return %a;
	
	return %b;
}

/// Creates a rotation matrix
function mRotationMatrix(%angle)
{
	%angle = mDegToRad(%angle);
	%sin   = mSin(%angle);
	%cos   = mCos(%angle);
	
	return %cos SPC -%sin SPC %sin SPC %cos;
}

/// Multiply a 2x2 matrix with a 2x1 vector
function mMatrixMultiply(%m, %v)
{
	%m11 = getWord(%m, 0);
	%m12 = getWord(%m, 1);
	%m21 = getWord(%m, 2);
	%m22 = getWord(%m, 3);
	
	%v1  = getWord(%v, 0);
	%v2  = getWord(%v, 1);
	
	%x   = %m11 * %v1 + %m12 * %v2;
	%y   = %m21 * %v1 + %m22 * %v2;
	
	return %x SPC %y;
}

/// Loops through all of the objects in a SimSet and returns them as a string
function SimSet::storeSet(%this)
{
	// Loop through all the objects and store them in a string
	%objectCount = %this.getCount();
	for (%i = 0; %i < %objectCount; %i++)
		%listString = %listString SPC %this.getObject(%i);
	
	// Trim and return
	return trim(%listString);
}

/// Loop through objects in a SimSet and make sure they are on the specified list.
/// It will also attempt to re-add removed items not in the set.
function SimSet::restoreSet(%this, %objectList, %delete, %restore)
{
	if (%delete  $= "") %delete  = true;
	if (%restore $= "") %restore = true;
	
	// Delete new items
	for (%i = 0; %i < %this.getCount(); %i++)
	{
		// Grab the object
		%object = %this.getObject(%i);
		
		// Check if it is part of the original list
		if (!sInString(%object, %objectList))
		{
			// Remove it from the set
			%this.remove(%object);
			
			// Delete it if requested
			if (%delete)
				%object.safeDelete();
			
			// Decrement the counter
			%i--;
		}
	}
	
	// Attempt to restore old items
	if (%restore)
	{
		for (%i = 0; %object < getWordCount(%objectList); %i++)
		{
			// Grab the object
			%object = getWord(%objectList, %i);
			
			// Check if it is still part of the set
			if (!%this.isMember(%object))
			{
				// Check if it still exists
				if (!isObject(%object))
					continue;
				
				// Re-add it to the list
				%this.add(%object);
			}
		}
	}
}

