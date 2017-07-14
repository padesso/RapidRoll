//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Controller - This behavior translate the input from the user into the
//				 actions of its owner. All a controller does is provide a brain
//				 for the body, telling it which direction to face, when to jump
//				 or when to attack.
//-----------------------------------------------------------------------------

if (!isObject(ControllerBehavior))
{
	%template = new BehaviorTemplate(ControllerBehavior);
	
	%template.friendlyName	= "Player Controller";
	%template.behaviorType	= "Actor";
	%template.description	= "Player Controller";
	
	%template.addBehaviorField( keyLeft,	"Move Left",	KEYBIND,	"keyboard left");
	%template.addBehaviorField( keyRight,	"Move Right",	KEYBIND,	"keyboard right");
	%template.addBehaviorField( keyUp,		"Move Up",		KEYBIND,	"keyboard up");
	%template.addBehaviorField( keyDown,	"Move Down",	KEYBIND,	"keyboard down");
	%template.addBehaviorField( keyJump,	"Jump",			KEYBIND,	"keyboard space");
	%template.addBehaviorField( keyAttack,	"Attack",		KEYBIND,	"keyboard X");
}

/// Set up the player's controller
function ControllerBehavior::onAddToScene(%this, %scenegraph)
{
	if (!%this.Owner.getBehavior(ActorBehavior))
		return;
	
	// Record the controller's id
	%this.Owner.Controller = %this;
	
	// Initialise the direction
	%this.Direction = 0 SPC 0;
	
	// Set the collision details
	%this.Owner.setObjectType("PlayerObject");
	%this.Owner.setCollidesWith("SolidPlatform EnemyObject PlayerTrigger");
	
	// Make sure we have a move map
	if (!isObject(moveMap))
		return;
	
	// Bind the appropriate keys
	moveMap.bindCmd(getWord(%this.keyLeft, 0),	getWord(%this.keyLeft, 1),	%this @ ".keyDown(Left);",		%this @ ".keyUp(Left);" );
	moveMap.bindCmd(getWord(%this.keyRight, 0),	getWord(%this.keyRight, 1),	%this @ ".keyDown(Right);",		%this @ ".keyUp(Right);");
	moveMap.bindCmd(getWord(%this.keyUp, 0),	getWord(%this.keyUp, 1),	%this @ ".keyDown(Up);",		%this @ ".keyUp(Up);" );
	moveMap.bindCmd(getWord(%this.keyDown, 0),	getWord(%this.keyDown, 1),	%this @ ".keyDown(Down);",		%this @ ".keyUp(Down);" );
	moveMap.bindCmd(getWord(%this.keyJump, 0),	getWord(%this.keyJump, 1),	%this @ ".keyDown(Jump);",		%this @ ".keyUp(Jump);" );
	moveMap.bindCmd(getWord(%this.keyAttack, 0),getWord(%this.keyAttack, 1),%this @ ".keyDown(Attack);",	%this @ ".keyUp(Attack);" );
	
	// Make sure the controller works
	moveMap.push();
}

/// A key is pressed
function ControllerBehavior::keyDown(%this, %keyDown)
{
	// Left key
	if (%keyDown $= "Left")
		%this.Direction.X = -1;
	
	// Right key
	if (%keyDown $= "Right")
		%this.Direction.X =  1;
	
	// Up key
	if (%keyDown $= "Up")
		%this.Direction.Y = -1;
	
	// Down key
	if (%keyDown $= "Down")
		%this.Direction.Y =  1;
		
	// Jump key
	if (%keyDown $= "Jump")
	{
		// Note that the jump key is being held
		%this.Jump = true;
		
		// Record the jump time
		%this.Owner.ActorBehavior.JumpTime = getSceneTime();
		
		// Which type of jump
		if (%this.Direction.Y > 0 && %this.Owner.ActorBehavior.isMethod("jumpDown"))
			%this.Owner.ActorBehavior.jumpDown();
		else if (%this.Owner.ActorBehavior.isMethod("jumpUp"))
			%this.Owner.ActorBehavior.jumpUp();
	}
	
	// Attak key
	if (%keyDown $= "Attack")
		%this.Attack = true;
}

/// A key is released
function ControllerBehavior::keyUp(%this, %keyUp)
{
	// Left key
	if (%keyUp $= "Left"  && %this.Direction.X == -1)
		%this.Direction.X = 0;
	
	// Right key
	if (%keyUp $= "Right" && %this.Direction.X == 1)
		%this.Direction.X = 0;
		
	// Up key
	if (%keyUp $= "Up")
		%this.Direction.Y = 0;
	
	// Down key
	if (%keyUp $= "Down")
		%this.Direction.Y = 0;
	
	// Jump key
	if (%keyUp $= "Jump")
		%this.Jump = false;
	
	// Attack key
	if (%keyUp $= "Attack")
		%this.Attack = false;
}