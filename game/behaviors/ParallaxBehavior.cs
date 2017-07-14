//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Parallax Scroller - A parallax scroller is a background that scrolls based
//						on the speed of the target. Use a higher scroll rate
//						for objects that should appear further away, and a low
//						rate for closer objects.
//-----------------------------------------------------------------------------

if (!isObject(ParallaxBehavior))
{
	%template = new BehaviorTemplate(ParallaxBehavior);
	
	%template.friendlyName = "Parallax Scroller";
	%template.behaviorType = "Miscellaneous";
	%template.description  = "Parallax Object";
	
	%template.addBehaviorField(	ScrollRate,	"Scroll Factor",	POINT2F,	"0.0 0.0");
}

function ParallaxBehavior::onAddToScene(%this, %scenegraph)
{
	// Enables the scroller to scroll
	%this.owner.enableUpdateCallback();
	
	%this.PreviousScrollPosition = 0 SPC 0;
}

function ParallaxBehavior::onUpdate(%this)
{
	// Check if we have an object to reference
	if (!$ParallaxScrollTarget)
		return;
	
	// Desired scroll rate
	%targetRate  = mVectorMultiply(mVectorMultiply($ParallaxScrollTarget.LinearVelocity, %this.ScrollRate), -1);
	%currentRate = %this.Owner.getScrollX() SPC %this.Owner.getScrollY();
	
	if (%currentRate !$= %targetRate)
	{
		if (VectorLen(%targetRate) != 0)
		{
			// Changing the scroll position will ensure that changes in speed is less jerky
			%cPosition   = 0 SPC 0;
			%cPosition.X = %this.Owner.getScrollPositionX();
			%cPosition.Y = %this.Owner.getScrollPositionY();
			
			%dPosition = mVectorMultiply(t2dVectorAdd(%cPosition, %this.PreviousScrollPosition), 0.5);
			
			%nPosition   = 0 SPC 0;
			%nPosition.X = %dPosition.X * (%this.Owner.Size.X / %this.Owner.RepeatX);
			%nPosition.Y = %dPosition.Y * (%this.Owner.Size.Y / %this.Owner.RepeatY);
			
			%this.Owner.setScrollPosition(%nPosition);
		}
		
		// Update the scroll rate
		%this.Owner.setScroll(%targetRate);
	}
	
	%this.PreviousScrollPosition.X = %this.Owner.getScrollPositionX();
	%this.PreviousScrollPosition.Y = %this.Owner.getScrollPositionY();
}