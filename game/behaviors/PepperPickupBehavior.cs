//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Pepper Pickup - This provides health to Players! (Must be added to a pick-
//					up object)
//-----------------------------------------------------------------------------

if (!isObject(PepperPickupBehavior))
{
	%template = new BehaviorTemplate(PepperPickupBehavior);
	
	%template.friendlyName	= "Pepper Pick-up";
	%template.behaviorType	= "Collectable";
	%template.description	= "This pepper restores health";
	
	%template.addBehaviorField( HealAmount,	"Amount of health restored",	INT,	10);
}

function PepperPickupBehavior::onAddToScene(%this, %scenegraph)
{
	if (%this.Owner.isMemberOfClass(t2dAnimatedSprite))
	{	
		%Animation = %this.Owner.AnimationName;
		%FrameCount = getWordCount(%Animation.AnimationFrames);
		
		%randomFrame = mCeil((%FrameCount - 1) * getRandom());
		
		%this.Owner.setAnimationFrame(%randomFrame);
	}
}

function PepperPickupBehavior::confirmPickup(%this, %targetObject, %inventoryItem)
{
	if (!%targetObject.ActorBehavior)
		return false;
	
	if (%targetObject.ActorBehavior.isMethod("healDamage"))
		%targetObject.ActorBehavior.healDamage(10);
	
	if (isObject(pepperPickupSound))
		playSound(PepperPickupSound);
		
	return true;
}