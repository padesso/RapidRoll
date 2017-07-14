//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Drill Methods -	All of the methods that makes a Drill a Drill.
//-----------------------------------------------------------------------------

function DrillClass::onAddToScene(%this)
{
	if (isObject(DrillHeadTemplate))
	{
		%this.DrillHead = new t2dSceneObject()
		{
			Config     = DrillHeadTemplate;
			Scenegraph = %this.Scenegraph;
			
			FlipX = %this.FlipX;
		};
		
		%this.DrillHead.mount(%this);
	}
}

function DrillClass::onRemove(%this)
{
	if (isObject(%this.DrillHead))
		%this.DrillHead.safeDelete();
}

function DrillClass::onRespawn(%this, %dAmount, %srcObject)
{
	%this.ActorBehavior.DisableGravity = false;
}

function DrillClass::onDeath(%this, %dAmount, %srcObject)
{
	%this.ActorBehavior.DisableGravity = true;
	if (isObject(%this.DrillHead))
		%this.DrillHead.safeDelete();
}

function DrillClass::hitWall(%this, %wall, %normal)
{
	%this.Controller.Direction.X = %normal.X;
}