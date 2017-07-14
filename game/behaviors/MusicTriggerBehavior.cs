//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Music Trigger - This trigger will play the specified music when the player
//					passes through. You can also specify "alternate" music
//					to be triggered. For example: MusicA is playing and has
//					an alternate specified as MusicB. When the actor passes
//					through it will play MusicB. Now that MusicB is playing,
//					when the actor passes back through, it will play MusicA.
//					Alternates are specified in the AudioDatablock under the
//					field name "Alternate".
//-----------------------------------------------------------------------------

if (!isObject(MusicTriggerBehavior))
{
	%template = new BehaviorTemplate(MusicTriggerBehavior);
	
	%template.friendlyName	= "Music Trigger";
	%template.behaviorType	= "Miscellaneous";
	%template.description	= "Transition music based on entry/exit direction";
	
	%template.addBehaviorField( Axis,			"Transitions trigger with X or Y axis",		ENUM, 		"X-Axis", "X-Axis" NL "Y-Axis");
	%template.addBehaviorField( PlayOnce,		"Play the music then disable the trigger",	BOOL,		FALSE);
	%template.addBehaviorField( MusicToPlay,	"The name of the datablock to play",		DEFUALT);
}

/// Set up the trigger
function MusicTriggerBehavior::onAddToScene(%this, %scenegraph)
{
	// Make sure we're a trigger
	if (%this.Owner.getClassName() !$= "t2dTrigger")
	{
		error("MusicTrigger must be used with a t2dTrigger object");
		return;
	}
	
	// Make sure we check events properly
	%this.Owner.setEnterCallback(1);
	%this.Owner.setStayCallback (0);
	%this.Owner.setLeaveCallback(1);
	
	// Ensure that the player collides with this trigger
	%this.Owner.setObjectType("PlayerTrigger");
	%this.Owner.setCollidesWith("None");
	
	%this.EntryDirection = 0 SPC 0;
	%this.LeaveDirection = 0 SPC 0;
}

function MusicTriggerBehavior::onEnter(%this, %theirObject)
{
	// Record the entry points
	%this.EnterDirection.X = 2 * (%theirObject.Position.X > %this.Owner.Position.X) - 1;
	%this.EnterDirection.Y = 2 * (%theirObject.Position.Y > %this.Owner.Position.Y) - 1;
	
	// Find current music
	%this.EnterMusic = getMusicDescription();
}

function MusicTriggerBehavior::onLeave(%this, %theirObject)
{
	// Record the exit points
	%this.LeaveDirection.X = 2 * (%theirObject.Position.X > %this.Owner.Position.X) - 1;
	%this.LeaveDirection.Y = 2 * (%theirObject.Position.Y > %this.Owner.Position.Y) - 1;
	
	// Check if we should change the music
	%deltaDirection = VectorSub(%this.LeaveDirection, %this.EnterDirection);
	if ((%this.Axis $= "X-Axis" && %deltaDirection.X != 0) || (%this.Axis $= "Y-Axis" && %deltaDirection.Y != 0))
	{
		// The default sound to play is the one specified by the object
		%playMusic = %this.MusicToPlay;
		
		// If nothing has been specified and an alternate is available, use that instead
		if (!isObject(%playMusic) && isObject(%this.EnterMusic.Alternate))
			%playMusic = %this.EnterMusic.Alternate;
		
		// Transition the sound
		transitionMusic(%playMusic);
		
		// Destory the trigger if requested
		if (%this.PlayOnce)
			%this.Owner.safeDelete();
	}
}