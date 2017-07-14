//---------------------------------------------------------------------------------------------
// Torque Game Builder
// Copyright (C) GarageGames.com, Inc.
//---------------------------------------------------------------------------------------------
//
// This is the file you should define your custom datablocks that are to be used
// in the editor.
//

datablock t2dSceneObjectDatablock(DragonTemplate)
{
	Class				= "PlayerClass";
	Size				= "16.000 16.000";
	Layer				= "1";
	CollisionPolyList	= "-0.300 -0.250 0.300 -0.250 0.300 0.250 0.000 0.480 -0.300 0.250";
	_Behavior0			= "ActorBehavior";
	_Behavior1			= "ControllerBehavior";
	_Behavior2			= "ActorAnimationBehavior	DatablockPrefix	Dragon	ScaleVolume	0";
};

datablock t2dSceneObjectDatablock(DrillTemplate)
{
	Class 			  	= "DrillClass";
	Size 			  	= "16.000 12.000";
	Layer 			  	= "2";
	CollisionPolyList	= "-0.550 -0.500 0.170 -0.500 0.170 0.220 -0.190 0.700 -0.550 0.220";
	_Behavior0 		  	= "ActorBehavior	MaxMoveSpeed	16	GroundAccel	16	GroundDecel	16	AllowJumpDown	0	AllowRespawn	0	HideOnDeath	1";
	_Behavior1 		  	= "AIControllerBehavior	AIType	Drill";
	_Behavior2 		  	= "ActorAnimationBehavior	DatablockPrefix	Drill	RunAnim	DrillIdleAnimation";
};

datablock t2dSceneObjectDatablock(DrillHeadTemplate)
{
	Size 				= "16.000 12.000";
	CollisionPolyList	= "0.170 -0.400 0.700 0.000 0.170 0.150";
	_Behavior0 			= "AreaDamageBehavior	PlayerOnly	1	Amount	20";
};

datablock t2dSceneObjectDatablock(PepperTemplate)
{
	AnimationName		= "yummy_pepperAnimation";
	Size 			  	= "6.000 6.000";
	Layer 			  	= "5";
	_Behavior0 		  	= "PickupBehavior";
	_Behavior1 		  	= "PepperPickupBehavior";
};

new GuiControlProfile(GuiExtraLifeCounter)
{
	fontType  = "David";
	fontColor = "255 255 255";
	fontSize  = 72;
};