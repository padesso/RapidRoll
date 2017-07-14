//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Object Manager - The object manager is basically a conditional collision
//					 system. It labels graph groups to make managing the
//					 collision system easy. To create an object type you must
//					 first register it as a new type. After registering an type
//					 you then assign it to any t2dSceneObject. Objects will
//					 only collide with the object types that they are allowed
//					 to.
//
//					 This script is not limited to the PSK and should be usable
//					 in any of your other projects.
//-----------------------------------------------------------------------------

// Initialise the number of types
$ObjectMethod::ObjectType = 0;

function getObjectTypeCount()		{ return $ObjectMethod::ObjectType; }
function getObjectTypeList(%type)	{ return $ObjectMethod::ObjectList[%type]; }
function getObjectTypeGroup(%type)	{ return $ObjectMethod::ObjectType[%type]; }

function updateObjectType(%type, %value)	{ $ObjectMethod::ObjectType[%type] = %value; }

//-----------------------------------------------------------------------------

function newObjectType(%type, %value)
{
	if (isObject($ObjectMethod::ObjectList[%type]))
		return;
	
	// New list
	$ObjectMethod::ObjectList[%type] = new SimSet();
	
	// Record the type and group
	$ObjectMethod::ObjectType[%type]  = %value;
	$ObjectMethod::ObjectType[%value] = %type;
	$ObjectMethod::ObjectType++;
}

/// Create a new object type
function registerObjectType(%types)
{
	// Get the number of new types to be added
	%typeCount = getWordCount(%types);
	
	// Get the last type in the list
	%childType  = getWord(%types, %typeCount - 1);
	%childValue = getObjectTypeCount() + 1;
	
	// Add it to the list of types
	newObjectType(%childType, %childValue);
	
	if (%typeCount > 1)
	{
		for (%i = 0; %i < %typeCount - 1; %i++)
		{
			// Get the first type and value
			%parentType  = getWord(%types, %i);
			%parentValue = getObjectTypeGroup(%parentType);
			
			// If it is a new one, add it
			if (%parentValue $= "")
			{
				%parentValue = getObjectTypeCount() + 1;
				newObjectType(%parentType, %parentValue);
			}
			
			// Record the parent's details
			%parentListId = %parentListId SPC getWord(%parentValue, 0);
			
			// Add the child's type to the parent's id
			updateObjectType(%parentType, %parentValue SPC %childValue);
			updateObjectType(%parentValue, %parentType);
		}
		
		// Define the child's group to include it's parents
		updateObjectType(%childType, %childValue SPC trim(%parentListId));
	}
}

//-----------------------------------------------------------------------------
// Scene Object Methods
//-----------------------------------------------------------------------------

/// Returns an objects type
function t2dSceneObject::getObjectType(%this)
{
	return %this.ObjectType;
}

/// Sets the target object to the type specified
function t2dSceneObject::setObjectType(%this, %assignedType)
{
	// Check to see if that object type has been registered
	if (!getObjectTypeGroup(%assignedType))
	{
		error ("No object of type \"" @ %assignedType @ "\" detected");
		return;
	}
	
	if (%this.ObjectType !$= "")
		%this.clearObjectType();
	
	// For each of the types in the tree, add it to their list
	%typeTree = getObjectTypeGroup(%assignedType);
	for (%i = 0; %i < getWordCount(%typeTree); %i++)
	{
		%typeName = getObjectTypeGroup(getWord(%typeTree, %i));
		getObjectTypeList(%typeName).add(%this);
	}
	
	// Assign the object's type
	%this.ObjectType = %assignedType;
	%this.setGraphGroup(getWord(%typeTree, 0));
}

/// Clear an objects type
function t2dSceneObject::clearObjectType(%this)
{
	// For each of the types in the tree, remove it to their list
	%typeTree = getObjectTypeGroup(%this.ObjectType);
	for (%i = 0; %i < getWordCount(%typeTree); %i++)
	{
		%typeName = getObjectTypeGroup(getWord(%typeTree, %i));
		getObjectTypeList(%typeName).remove(%this);
	}
	
	// Clear the object type
	%this.ObjectType = "";
	%this.setGraphGroup(0);
}

/// Define which object types the target object may collide with
function t2dSceneObject::setCollidesWith(%this, %typeList)
{
	// Collide with all groups?
	if (%typeList $= "All")
	{
		for (%i = 0; %i <= 31; %i++)
			%collisionList = %collisionList SPC %i;
			
		%collisionList = trim(%collisionList);
	}
	
	// Collide with specific groups
	if (%collisionList $= "" && %typeList !$= "None")
	{
		// Loop through all the types in the list
		%typeCount = getWordCount(%typeList);
		for (%i = 0; %i < %typeCount; %i++)
		{
			// Get the object type
			%objectType = getWord(%typeList, %i);
			
			// Ensure it exists
			if (!getObjectTypeGroup(%objectType))
			{
				warn(%objectType SPC "ommited from collision group, doesn't exist");
				continue;
			}
				
			%collisionList = %collisionList SPC getObjectTypeGroup(%objectType);
		}
		
		// Trim the white space
		%collisionList = trim(%collisionList);
	}
	
	// Set the collision groups
	%this.setCollisionGroups(%collisionList);
}