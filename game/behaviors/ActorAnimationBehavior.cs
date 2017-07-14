//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Animation Manager - The animation manager is a Finite State Machine which
//						tells the scene object which animations to play and
//						the appropriate sounds which accompany them. It must
//						be added to an actor AND must be on an animated sprite.
//-----------------------------------------------------------------------------

if (!isObject(ActorAnimationBehavior))
{
	%template = new BehaviorTemplate(ActorAnimationBehavior);
	
	%template.friendlyName	= "Actor Animation Manager";
	%template.behaviorType	= "Actor";
	%template.description	= "Datablocks must be in format: PREFIX STATE ANIMATION/SOUND (no spaces)";
	
	%template.addBehaviorField( DatablockPrefix,	"Datablock naming convention: PREFIX", DEFAULT, "");
	
	%template.addBehaviorField( AllowTransitions,   "Transition Animations",	BOOL,	true);
	
	%template.addBehaviorField( PlaySounds, 		"Play Sounds", 		 							BOOL,	true);
	%template.addBehaviorField( ScaleVolume, 		"Scale volume based on distance from camera",	BOOL,	true);
	%template.addBehaviorField( SoundMinDistance,	"Distance to camera before sound fades",		INT,	60.0);
	%template.addBehaviorField( SoundMaxDistance,	"Distance to camera before sound stops",		INT,	100.0);
	
	%template.addBehaviorField( IdleAnim,   	"Idle Animation",		OBJECT, "IdleAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( RunAnim,		"Running Animation",	OBJECT, "RunAnimation",			t2dAnimationDatablock);
	%template.addBehaviorField( RunJumpAnim,	"Run Jump Animation",	OBJECT, "RunJumpAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( RunFallAnim,	"Run Fall Animation",	OBJECT, "RunFallAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( SlideAnim,		"Sliding Animation",	OBJECT, "SlideAnimation",		t2dAnimationDatablock);
	
	%template.addBehaviorField( JumpAnim,		"Jump Animation",		OBJECT, "JumpAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( FallAnim,		"Fall Animation",		OBJECT, "FallAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( GlideAnim,		"Glide Animation",		OBJECT, "GlideAnimation",		t2dAnimationDatablock);
	
	%template.addBehaviorField( ClimbIdleAnim,	"Climb Idle Animation",	OBJECT, "ClimbIdleAnimation",	t2dAnimationDatablock);
	%template.addBehaviorField( ClimbUpAnim,	"Climb Up Animation",	OBJECT, "ClimbUpAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( ClimbDownAnim,	"Climb Down Animation",	OBJECT, "ClimbDownAnimation",	t2dAnimationDatablock);
	%template.addBehaviorField( ClimbJumpAnim,	"Climb Jump Animation",	OBJECT, "ClimbJumpAnimation",	t2dAnimationDatablock);
	
	%template.addBehaviorField( ActionAnim,		"Action Animation",		OBJECT, "ActionAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( DamageAnim,		"Damage Animation",		OBJECT, "DamageAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( SpawnAnim,		"Spawn Animation",		OBJECT, "SpawnAnimation",		t2dAnimationDatablock);
	%template.addBehaviorField( DieAnim,		"Death Animation",		OBJECT, "DieAnimation",			t2dAnimationDatablock);
}

/// Set up some of the things needed for the animation manager
function ActorAnimationBehavior::onAddToScene(%this, %scenegraph)
{
	// We cannot animate the actor if it isn't an animated sprite
	if (!%this.Owner.isMemberOfClass(t2dAnimatedSprite))
	{
		error("Animation Manager must be added to an t2dAnimatedSprite");
		return;
	}
	
	// Assign the animation manager
	%this.Owner.AnimationManager = %this;
	
	// State manager
	%this.StateManager = new ScriptObject();
	
	// Main animations
	%this.registerState("idle");
	%this.registerState("jump");
	%this.registerState("fall");
	%this.registerState("glide");
	%this.registerState("run");
	%this.registerState("climbidle");
	%this.registerState("climbup");
	%this.registerState("climbdown");
	%this.registerState("damage");
	%this.registerState("spawn");
	%this.registerState("die");
	
	// Secondary Animations
	%this.registerState("slide");
	%this.registerState("action");
	%this.registerState("climbjump", "jump");
	%this.registerState("runjump", "jump");
	%this.registerState("runfall", "fall");
	
	// Initialise states
	%this.CurrentState  = "idle";
	%this.PreviousState = NULL;
	
	// Animate the new object if possible
	if (isObject(%this.StateManager.getFieldValue("spawn")))
	{
		// Update the properties
		%this.Owner.ActorBehavior.Alive    = false;
		%this.Owner.ActorBehavior.Spawning = true;
		
		// Set the state
		%this.setState("spawn");
	}
}

/// Ensure that any sounds that are playing are stopped when this object is removed
function ActorAnimationBehavior::onRemove(%this, %scenegraph)
{
	%this.stopSound();
}

/// Register an animation state
function ActorAnimationBehavior::registerState(%this, %state, %fallback)
{
	// This function registers different animation states and also allows you to
	// add a "fallback" animation. So, if an animation doesn't exist for that
	// state, it will fall back on the specified animation.
	
	// You have a two options for setting up your animation states:
	
	// 1:	Have a naming convention that looks like this:
	//		DatablockPrefix StateName ObjectType
	//
	//		For example: Specify your DatablockPrefix as "Dragon" then you should set
	//					 up the Idle state by naming the animation
	//					 "DragonIdleAnimation". If you want a sound to be associated
	//					 with an animation replace "Animation" with "Sound".
	
	// 2:	Specify each state individually. It pays to be consistent, however.
	//		Animations still use the DatablockPrefix field, so you will still need
	//		your animations in the format:
	//		DataBlockPrefix InsertSomethingHere ObjectType
	//
	//		For example: Say you want your "damage" state to use the animation
	//		"DragonSpazAnimation", then you will need to specify this animation for
	//		the damage state. The sound "DragonSpazSound" will be used in this case
	//		as well.
	
	//	Animation transitions are in the format:
	//	DataBlockPrefix StateFrom _to_ StateTo ObjectType
	//
	//	For example: Idle to run looks like this "DragonIdle_to_RunAnimation"
	
	// Step sounds are sounds that are played on particular frames. You can specify
	// an animation to use step sounds by inserting the dynamic field "stepFrames"
	// into your animation datablock and then specify which frames you want the
	// sound played in.
	//
	// For example: %runDatablock.stepFrames = "5 11";
	
	// Grab the datablock name for this state
	%animationDatablock = %this.getFieldValue(%state @ "Anim");
	
	// Check if we have a fallback and an animation for this state
	if (%fallback !$= "" && !isObject(%animationDatablock))
		%animationDatablock = %this.getFieldValue(%fallback @ "Anim");
	
	// Check if we can just use default settings for this state
	if (!isObject(%animationDatablock))
		%animationDatablock = %this.DatablockPrefix @ %state @ "Animation";
	
	// If it doesn't exist, we don't want to register the animation
	if (!isObject(%animationDatablock))
		return;
		
	// Update the field just in case we've changed it
	%this.setFieldValue(%state @ "Anim", %animationDatablock);
	
	// Register the animation with the datablock
	%this.StateManager.setFieldValue(%state, %animationDatablock);
}

/// Unregister an animation state
function ActorAnimationBehavior::unregisterState(%this, %state)
{
	// Clear the state
	%this.StateManager.setFieldValue(%state, "");
}

/// Update the actor's animation and direction
function ActorAnimationBehavior::updateAnimation(%this)
{
	// Grab the possible state
	eval("%targetState = " @ %this.CurrentState @ "State::execute(%this);");
	
	// Set new state
	%this.setState(%targetState);
	
	// If we're dead or spawning, just return
	if (%this.CurrentState $= "die" || %this.CurrentState $= "spawn")
		return;
		
	// If there is a sound playing, make sure we update the volume
	%soundCue = %this.getSoundHandle();
	if (getIsLooping(%soundCue))
		%this.setHandleVolume(%soundCue);
	
	// Make sure we face in the correct direction
	if (!(isObject(%this.SlideAnim) && %this.CurrentState $= "slide"))
	{
		if (%this.Owner.Controller.Direction.X > 0 || %this.Owner.ActorBehavior.Climbing)
			%this.Owner.FlipX = false;
		else if (%this.Owner.Controller.Direction.X < 0)
			%this.Owner.FlipX = true;
	}
	else
	{
		%moveSpeed		   = %this.Owner.ActorBehavior.MoveSpeed.X;
		%inheritedVelocity = %this.Owner.ActorBehavior.InheritedVelocity.X;
		%groundVelocity    = %this.Owner.ActorBehavior.GroundObject.LinearVelocity.X;
		
		%this.Owner.FlipX  = (%moveSpeed + %inheritedVelocity - %groundVelocity < 0);
	}
}

/// Animates the actor and plays any sounds required
function ActorAnimationBehavior::AnimateActor(%this, %animation, %frame)
{
	if (%animation $= NULL || !isObject(%animation))
		return;
	
	// Play the new animation
	%frame = (%frame < getWordCount(%animation.AnimationFrames)) ? %frame : 0;
	%this.Owner.playAnimation(%animation);
	%this.Owner.setAnimationFrame(%frame);
	
	// If we're making sounds at certain frames, then enable the frame change callback
	%useStepFrames = (%animation.stepFrames !$= "") ? true : false;
	%this.Owner.setFrameChangeCallback(%useStepFrames);
	
	// Find the soundblock for this animation
	%soundBlock = %this.findSoundblock(%animation);
	
	// Play the target sound
	if (isObject(%soundBlock) && !%useStepFrames)
		%this.playSound(%soundBlock);
}

/// If we're using step sounds, then this is where they are played
function ActorAnimationBehavior::onFrameChange(%this, %frame)
{
	// Find the soundblock for this animation
	%animation  = %this.Owner.getAnimation();
	%soundBlock = %this.findSoundblock(%animation);
	
	%frameCount = getWordCount(%animation.stepFrames);
	for (%i = 0; %i < %frameCount; %i++)
	{
		// Find the step frames list
		%stepFrame = getWord(%animation.stepFrames, %i);
		
		// Check if we need to play a sound in this frame
		if (%frame == %stepFrame && isObject(%soundBlock))
			%this.playSound(%soundBlock);
	}
}

/// Sets the current state of the actor
function ActorAnimationBehavior::setState(%this, %targetState)
{
	// If we don't need to change the state or its not valid, return
	if (%targetState $= NULL || !isObject(%this.StateManager.getFieldValue(%targetState)))
		return;
	
	// Exit out of the animation
	%this.exit();
	
	// Update the state
	%this.PreviousState = %this.CurrentState;
	%this.CurrentState  = %targetState;
	
	// Enter the new one
	eval (%this.CurrentState @ "State::enter(%this);");
}

/// Find the transition animations
function ActorAnimationBehavior::getTransition(%this, %from, %to)
{
	if (!%this.AllowTransitions)
		return NULL;
	
	// Find the transition
	%transition = %this.DatablockPrefix @ %from @ "_to_" @ %to @ "Animation";
	
	// If doesn't exist, return nothing
	if (!isObject(%transition))
		return NULL;
	
	// Return the transition
	return %transition;
}

/// Finds the sound block associated with the target animation
function ActorAnimationBehavior::findSoundblock(%this, %animation)
{
	// If we don't play sounds, return nothing
	if (!%this.PlaySounds)
		return NULL;
	
	%prefixLen  = strlen(%this.DatablockPrefix);
	%suffixLen  = strlen("Animation");
	
	// Find the sound block
	%stateName  = getSubStr(%animation, %prefixLen, strlen(%animation) - %prefixLen - %suffixLen);
	%soundBlock = %this.DatablockPrefix @ %stateName @ "Sound";
	
	// If there are multiple blocks, find a random one to play
	if (isObject(%soundBlock @ 0))
	{
		%i = 0;
		while (isObject(%soundBlock @ %i))
			%i++;
		
		%soundBlock = %soundBlock @ mFloor(getRandom() * %i);
	}
	
	// If there isn't an object for that name, return
	if (!isObject(%soundBlock))
		return NULL;
	
	// Return the soundblock
	return %soundBlock;
}

function ActorAnimationBehavior::playSound(%this, %datablock)
{
	// Use the current state if no datablock has been specified
	if (%datablock $= "")
	{
		%datablock = %this.findSoundblock(%this.CurrentState);
		if (!isObject(%datablock))
			return;
	}
	
	// Find the volume
	%initialVolume = %this.getScaledVolume();
	
	// Tell the sound manager to play the sound
	SimObject::playSound(%this, %datablock, %initialVolume);
}

/// Change the volume of any sounds produced based on the objects distance from the camera.
function ActorAnimationBehavior::getScaledVolume(%this)
{
	// Don't change the volume if we don't want to
	if (!%this.ScaleVolume)
		return 1.0;
	
	// Grab the positions
	%cameraPosition = sceneWindow2D.getCurrentCameraPosition();
	%actorPosition  = %this.Owner.Position;
	
	// Get the distance to the camera
	%distanceToCamera = VectorDist(%cameraPosition, %actorPosition);
	
	// Scale the volume and clamp it
	%volume = (%this.SoundMaxDistance - %distanceToCamera) / (%this.SoundMaxDistance - %this.SoundMinDistance);
	%volume = mClamp(%volume, 0.0, 1.0);
	
	// Return the volume
	return %volume;
}

function ActorAnimationBehavior::setHandleVolume(%this, %handle)
{
	// Find the scaled volume
	%volume = %this.getScaledVolume();
	
	// Return if it is at its target
	if (%volume == getHandleVolume(%handle))
		return;
	
	setHandleVolume(%handle, %volume);
}

/// When the actor changes state, this function is called to do a few checks
function ActorAnimationBehavior::enter(%this)
{
	// Find the transition
	%transition = %this.getTransition(%this.PreviousState, %this.CurrentState);
	
	// If there is one, make sure we transition to the new animation
	if (%transition !$= NULL)
	{
		%this.Transitioning = true;
		%this.AnimateActor(%transition, 0);
	}
	
	// Ensure that we're visible if we're alive
	if (%this.Owner.ActorBehavior.Alive)
	{
		%this.Owner.Visible = true;
	}
}

/// This function is done when an animation is updated
function ActorAnimationBehavior::execute(%this)
{
	// Move to the new state's animation
	if (%this.Transitioning && %this.Owner.getIsAnimationFinished())
	{
		%this.Transitioning = false;
		%this.AnimateActor(%this.TransitioningTo, 0);
	}
}

/// This is called when we leave a state
function ActorAnimationBehavior::exit(%this)
{
	// If there is a sound playing, make sure we update the volume
	%soundCue = %this.getSoundHandle();
	if (getIsLooping(%soundCue))
		%this.stopSound();
	
	// Stop transitioning if we're forced to
	%this.Transitioning = false;
}

////////////////////////////////////////////////////////////////////////////////
/// IDLE STATE
////////////////////////////////////////////////////////////////////////////////

function IdleState::enter(%this)
{
	%this.enter();
	
	%this.IdleTime = getSceneTime();
	
	%this.TransitioningTo = %this.IdleAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function IdleState::execute(%this)
{
	%this.execute();
	
	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if ((getSceneTime() - %this.Owner.ActorBehavior.GroundTime) > 0.1)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			return "fall";
		}
	}
	
	%inheritedVelocity = %this.Owner.ActorBehavior.InheritedVelocity.X;
	%groundVelocity    = %this.Owner.ActorBehavior.GroundObject.LinearVelocity.X;
	if (isObject(%this.SlideAnim) && %moveSpeed == 0 && %inheritedVelocity != %groundVelocity)
		return "slide";

	if (%this.Owner.Controller.Direction.X != 0)
		return "run";
		
	if (getSceneTime() - %this.IdleTime > 1.2)
		return "action";
		
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// RUN STATE
////////////////////////////////////////////////////////////////////////////////

function RunState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.RunAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function RunState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if ((getSceneTime() - %this.Owner.ActorBehavior.GroundTime) > 0.1)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			return "fall";
		}
	}
	
	%moveSpeed = %this.Owner.ActorBehavior.MoveSpeed.X;
	if (isObject(%this.SlideAnim) && (%moveSpeed > 0 && %this.Owner.Controller.Direction.X < 0)
		|| (%moveSpeed < 0 && %this.Owner.Controller.Direction.X > 0))
		return "slide";
	
	if (%this.Owner.Controller.Direction.X == 0)
		return "idle";
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// RUN JUMP STATE
////////////////////////////////////////////////////////////////////////////////

function RunJumpState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.RunJumpAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function RunJumpState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if (!%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
			return "runFall";
	}
	else
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
		
		return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// RUN FALL STATE
////////////////////////////////////////////////////////////////////////////////

function RunFallState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.RunFallAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function RunFallState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if (%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
		
		return "idle";
	}
	else
	{
		if (%this.Owner.ActorBehavior.IsGliding)
			return "glide";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// SLIDE STATE
////////////////////////////////////////////////////////////////////////////////

function SlideState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.SlideAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function SlideState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if ((%this.Owner.ActorBehavior.MoveSpeed.X >= 0) == (%this.Owner.Controller.Direction.X >= 0))
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
			
		%groundVelocity = %this.Owner.ActorBehavior.GroundObject.LinearVelocity.X;
		%groundForce	= %this.Owner.ActorBehavior.GroundObject.SurfaceForce.X;
		if (%this.Owner.LinearVelocity.X == (%groundVelocity + %groundForce))
			return "idle";
	}
	
	if (!%this.Owner.ActorBehavior.onGround && %this.Owner.LinearVelocity.Y > 0)
		return "runFall";
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// JUMP STATE
////////////////////////////////////////////////////////////////////////////////

function JumpState::enter(%this)
{
	%this.enter();
	
	%this.TransitioningTo = %this.JumpAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function JumpState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if (!%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			return "fall";
		}
	}
	else
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
		
		return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// FALL STATE
////////////////////////////////////////////////////////////////////////////////

function FallState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.FallAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function FallState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if (%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
		
		return "idle";
	}
	else
	{
	   //Hook into animation system to update score and test for win
	   $score += 3;
	   
	   if($score >= $winScore)
	   {	      
	      $score = $winScore;
	      setScore($score);  
	      
	      levelComplete();
	   }
	   else
	   {
	    setScore($score);  
	   }
	   
		if (%this.Owner.ActorBehavior.IsGliding)
			return "glide";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// GLIDE STATE
////////////////////////////////////////////////////////////////////////////////

function GlideState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.GlideAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function GlideState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		
		return "climbIdle";
	}
	
	if (%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.Controller.Direction.X != 0)
			return "run";
		
		return "idle";
	}
	else
	{
		if (!%this.Owner.ActorBehavior.IsGliding)
			return "fall";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// CLIMB IDLE STATE
////////////////////////////////////////////////////////////////////////////////

function ClimbIdleState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.ClimbIdleAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function ClimbIdleState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
	}
	else
	{
		if (!%this.Owner.ActorBehavior.onGround)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			return "fall";
		}
		else
			return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// CLIMB UP STATE
////////////////////////////////////////////////////////////////////////////////

function ClimbUpState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.ClimbUpAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function ClimbUpState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
		else if (%this.Owner.LinearVelocity.Y == 0)
			return "climbIdle";
	}
	else
	{
		if (!%this.Owner.ActorBehavior.onGround)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			return "fall";
		}
		else
			return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// CLIMB DOWN STATE
////////////////////////////////////////////////////////////////////////////////

function ClimbDownState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.ClimbDownAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function ClimbDownState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y == 0)
			return "climbIdle";
	}
	else
	{
		if (!%this.Owner.ActorBehavior.onGround)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
				
			return "fall";
		}
		else
			return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// CLIMB JUMP STATE
////////////////////////////////////////////////////////////////////////////////

function ClimbJumpState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.ClimbJumpAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function ClimbJumpState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.ActorBehavior.Climbing)
	{
		if (%this.Owner.LinearVelocity.Y < 0)
			return "climbUp";
		else if (%this.Owner.LinearVelocity.Y > 0)
			return "climbDown";
			
		return "climbIdle";
	}
	
	if (!%this.Owner.ActorBehavior.onGround)
	{
		if (%this.Owner.LinearVelocity.Y > 0)
		{
			if (%this.Owner.ActorBehavior.IsGliding)
				return "glide";
			
			if (%this.Owner.Controller.Direction.X != 0)
				return "runFall";
			else
				return "fall";
		}
	}
	else
	{
		if (%this.Owner.Controller.Direction.X != 0)
				return "run";
				
		return "idle";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// ACTION STATE
////////////////////////////////////////////////////////////////////////////////

function ActionState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.ActionAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function ActionState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.getIsAnimationFinished())
		return "idle";
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// DAMAGE STATE
////////////////////////////////////////////////////////////////////////////////

function DamageState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.DamageAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function DamageState::execute(%this)
{
	%this.execute();

	if (!%this.Owner.ActorBehavior.Alive)
		return "die";
		
	if (%this.Owner.getIsAnimationFinished())
		return "idle";
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// SPAWNING STATE
////////////////////////////////////////////////////////////////////////////////

function SpawnState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.SpawnAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function SpawnState::execute(%this)
{
	%this.execute();

	if (%this.Owner.getIsAnimationFinished())
	{
		%this.Owner.ActorBehavior.spawnFinished();
		return "fall";
	}
	
	return NULL;
}

////////////////////////////////////////////////////////////////////////////////
/// DIE STATE
////////////////////////////////////////////////////////////////////////////////

function DieState::enter(%this)
{
	%this.enter();
		
	%this.TransitioningTo = %this.DieAnim;
	
	if (!%this.Transitioning)
		%this.AnimateActor(%this.TransitioningTo, 0);
}

function DieState::execute(%this)
{
	%this.execute();

	if (%this.Owner.getIsAnimationFinished() && %this.Owner.ActorBehavior.HideOnDeath)
		%this.Owner.Visible = false;

	if (%this.Owner.ActorBehavior.Alive)
		return "idle";
	
	return NULL;
}