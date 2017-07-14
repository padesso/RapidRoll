//---------------------------------------------------------------------------------------------
// Torque Game Builder
// Copyright (C) GarageGames.com, Inc.
//---------------------------------------------------------------------------------------------

//---------------------------------------------------------------------------------------------
// startGame
// All game logic should be set up here. This will be called by the level builder when you
// select "Run Game" or by the startup process of your game to load the first level.
//---------------------------------------------------------------------------------------------
function startGame(%level)
{
	Canvas.setContent(mainScreenGui);
	Canvas.setCursor(DefaultCursor);
	
	// Disable the mouse
	Canvas.cursorOff();
	
	new ActionMap(moveMap);	
	moveMap.push();

   moveMap.bindCmd(keyboard, "escape", "quit();", "");
	
	$enableDirectInput = true;
	activateDirectInput();
	enableJoystick();
	
	// Loads various functions required through the kit
	exec ("./GameMethods.cs");
	initialisePlatformerKit();
	
	// After all the default settings are loaded, you would want to load the user's
	// custom settings here. For example: If the user changes their volume settings,
	// you would override the defaults set in the initialisation function.
	
	sceneWindow2D.loadLevel(%level);
}

//---------------------------------------------------------------------------------------------
// endGame
// Game cleanup should be done here.
//---------------------------------------------------------------------------------------------
function endGame()
{
	sceneWindow2D.endLevel();
	moveMap.pop();
	moveMap.delete();
}

function setScore(%newScore)
{
   guiScore.text = %newScore;
   
   if(%newScore >= 1000)
   {
      //force it to 1000
      thousandsDigit.setFrame(1); 
      hundredsDigit.setFrame(0);
      tensDigit.setFrame(0);
      onesDigit.setFrame(0);
   }
   else if(%newScore < 1000 && %newScore >= 100)
   {
      thousandsDigit.setFrame(0);
      hundredsDigit.setFrame(getSubStr(%newScore,0,1));
      tensDigit.setFrame(getSubStr(%newScore,1,1));
      onesDigit.setFrame(getSubStr(%newScore,2,1));
   }
   else if(%newScore < 100 && %newScore >= 10)
   {
      thousandsDigit.setFrame(0);
      hundredsDigit.setFrame(0);
      tensDigit.setFrame(getSubStr(%newScore,0,1));
      onesDigit.setFrame(getSubStr(%newScore,1,1));
   }
   else if(%newScore < 10)
   {
      thousandsDigit.setFrame(0);
      hundredsDigit.setFrame(0);
      tensDigit.setFrame(0);
      onesDigit.setFrame(getSubStr(%newScore,0,1));
   }
}
