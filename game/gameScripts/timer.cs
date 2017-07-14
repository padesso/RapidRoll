function startTimers()
{
   speedController.setTimerOn($scrollSpeedInterval);
    
   %spawnTime = getRandom($platformSpawnMin, $platformSpawnMax);  
   platformSpawner.setTimerOn(%spawnTime);   
}

function t2dSceneObject::onTimer(%this)
{
   //Speed things up as the time passes, clamp at max  
   if($scrollSpeed < $maxScrollSpeed)
   {
      $scrollSpeed += $scrollSpeedOffset;
   }
   else
   {
      $scrollSpeed = $maxScrollSpeed;
   }
   
   echo("New speed: " @ $scrollSpeed); 
   
   updateScrollers();
   
   speedController.setTimerOn($scrollSpeedInterval);
}

function PlatformSpawner::onTimer(%this)
{
   echo("Spawn a platform");  
   
   %chance = getRandom(0, $fallingPlatformChance);
   if(%chance < $fallingPlatformChance)
   {
      %safeClone = safePlatform.cloneWithBehaviors();
      %tempX = getRandom(-35, 35);
      %safeClone.setPosition(%tempX, 45);
      %safeClone.setLinearVelocityY(-$scrollSpeed);
      
      //Add clones to the simSet so we can keep scrollspeed updated
      $platformList.add(%safeClone);
      
      $lowestPlatform = %safeClone;
   }
   else
   {
      %fallingClone = fallingPlatform.cloneWithBehaviors();
      %tempX = getRandom(-35, 35);
      %fallingClone.setPosition(%tempX, 45);
      %fallingClone.setLinearVelocityY(-$scrollSpeed);
      
      //Add clones to the simSet so we can keep scrollspeed updated
      $platformList.add(%fallingClone);
   }  
   
   //reset the timer
   %spawnTime = getRandom($platformSpawnMin, $platformSpawnMax);
   platformSpawner.setTimerOn(%spawnTime);  
}
   