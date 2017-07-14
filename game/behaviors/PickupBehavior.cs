//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//	Pick-Up Item - In most games, there are objects which can be collected.
//				   Add this behavior to an object that you want to be
//				   collectable. The type of object that is to be picked up
//				   must be specified in another behavior (see PepperPickup or
//				   CheckPoint behaviors for more information).
//-----------------------------------------------------------------------------

if (!isObject(PickupBehavior))
{
	%template = new BehaviorTemplate(PickupBehavior);
	
	%template.friendlyName	= "Pick-up Item";
	%template.behaviorType	= "Collectable";
	%template.description	= "Pick up this item";
	
	%template.addBehaviorField( AddToInventory,	  "Add this item to inventory",			BOOL, 	false);
}

function PickupBehavior::onAddToScene(%this, %scenegraph)
{
	// Make sure this object doesn't collide
	%this.Owner.setCollisionActive(0,0);
	%this.Owner.setCollisionPhysics(0,0);
	%this.Owner.setCollisionCallback(0);
	
	// Create a trigger to handle the pickup
	%this.Owner.PickupTrigger = new t2dTrigger()
	{
		Scenegraph = %this.Owner.SceneGraph;
		Class	   = PickupTrigger;
		
		Size	   = %this.Owner.Size;
		Position   = %this.Owner.Position;
		
		EnterCallback = true;
		LeaveCallback = false;
		StayCallback  = false;
		
		Behavior = %this;
		Owner	 = %this.Owner;
	};
	
	// Make sure the trigger only collides with the player
	%this.Owner.PickupTrigger.setObjectType("PlayerTrigger");
	%this.Owner.PickupTrigger.setCollidesWith("None");
}

function PickupBehavior::onRemove(%this)
{
	// If we've been removed through alternate methods, remember to delete the trigger
	if (isObject(%this.Owner.PickupTrigger))
		%this.Owner.PickupTrigger.safeDelete();
}

function PickupTrigger::onEnter(%this, %theirObject)
{
	if (!%theirObject.Controller || !%theirObject.ActorBehavior)
		return;
	
	// Should it be added to the inventory?
	%inventoryItem = %this.Behavior.AddToInventory;
	
	// Add it to the inventory
	if (%inventoryItem)
	{
		// Find the inventory list
		%inventory = %theirObject.ActorBehavior.InventoryList;
		
		// Cancel pick-up if there are already too many items
		if (%inventory.getCount() == %inventory.MaxItems)
		{
			// Cannot pick up this item
			error ("Inventory Full");
			
			return;
		}
		
		// Hide the item
		%this.Owner.Visible = false;
		
		// If it was from a spawn point, remove it from the spawn point's list
		if (isObject(%this.Owner.SpawnPoint))
			%this.Owner.SpawnPoint.SpawnedObjects.remove(%this.Owner);
		
		// Add it to the inventory
		%inventory.add(%this.Owner);
	}
		
	// Notify other behaviors the object has been picked up
	%behaviorCount = %this.Owner.getBehaviorCount();
	for (%i = 0; %i < %behaviorCount; %i++)
	{
		// Get the ith behavior
		%behavior = %this.Owner.getBehaviorByIndex(%i);
		
		// Skip this one
		if (%behavior == %this)
			continue;
		
		// Check if we've been picked up or not
		if (%behavior.isMethod("confirmPickup"))
			%canDelete = %behavior.confirmPickup(%theirObject, %inventoryItem);
	}
	
	// Destroy the object and trigger
	if (%canDelete)
	{
		if (!%inventoryItem)
			%this.Owner.safeDelete();
		
		%this.safeDelete();
	}
}