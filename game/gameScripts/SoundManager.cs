//-----------------------------------------------------------------------------
//  Platformer Starter Kit
//  Copyright (C) Phillip O'Shea
//  
//  Sound Manager - This file contains generic methods which are designed to
//					make managing sounds in your project easy. If you specify
//					an object to play a sound, then the handle will be stored
//					by that object, otherwise it will be stored by the sound
//					manager. I have also included some generic methods to make
//					sound management easier.
//-----------------------------------------------------------------------------

// Stores sound/music information
$SoundManager = new ScriptObject();

// Manager default.
// Note: It would be a good idea to keep the channels constant.
// 		 Channels numbers are stored in the "Type" field of an Audio Description.
$SoundManager::Sound::Channel = 0;
$SoundManager::Sound::Volume  = 1.0;

$SoundManager::Music::Channel = 1;
$SoundManager::Music::Volume  = 1.0;

// Time in ms and the number of itterations (the more the smoother the transition)
$SoundManager::Transition::Time 	   = 1000;
$SoundManager::Transition::Itterations = 10;

function getSoundChannel()					{ return $SoundManager::Sound::Channel; }
function getMusicChannel()					{ return $SoundManager::Music::Channel; }

function playSound(%sound, %volume, %stop)	{ return $SoundManager.playSound(%sound, %volume, %stop); }
function stopSound()						{ return $SoundManager.stopSound(); }
function getSoundHandle()					{ return $SoundManager.getSoundHandle(); }
function getSoundDescription()				{ return $SoundManager.getSoundDescription(); }

function playMusic(%music, %volume, %stop)	{ return $SoundManager.playMusic(%music, %volume, %stop); }
function stopMusic()						{ return $SoundManager.stopMusic(); }
function getMusicHandle()					{ return $SoundManager.getMusicHandle(); }
function getMusicDescription()				{ return $SoundManager.getMusicDescription(); }

function transitionMusic(%newMusic)			{ return $SoundManager.transitionMusic(%newMusic); }

/// Set the volume of the target channel
function setChannelVolume(%channel, %volume)
{
	alxSetChannelVolume(%channel, %volume);
}

/// Retreives the channel volume
function getChannelVolume(%channel)
{
	alxGetChannelVolume(%channel);
}

/// Play the specified sound
function SimObject::playSound(%this, %sound, %volume, %stop)
{
	// Force the previous sound to stop playing?
	%stop = (%stop $= "") ? true : %stop;
	
	// Stop the current looping sound that is being played
	if (%stop && alxIsPlaying(%this.SoundHandle) && getIsLooping(%this.SoundHandle))
		%this.stopSound();
	
	// Record the datablock and handle id's
	%this.SoundDescription = %sound;
	%this.SoundHandle	   = alxPlay(%sound);
	
	// Set the volume
	setHandleVolume(%this.SoundHandle, (%volume $= "") ? 1.0 : %volume);
	
	return %this.SoundHandle;
}

/// Stop the current sound
function SimObject::stopSound(%this)
{
	if (!alxIsPlaying(%this.SoundHandle))
		return;
	
	// Stop the current sound
	alxStop(%this.SoundHandle);
}

/// Returns the current sound
function SimObject::getSoundHandle(%this)
{
	if (!alxIsPlaying(%this.SoundHandle))
		return NULL;
	
	// Return the handle
	return %this.SoundHandle;
}

/// Returns the current sound's description
function SimObject::getSoundDescription(%this)
{
	if (!alxIsPlaying(%this.SoundHandle))
		return NULL;
	
	// Return the datablock
	return %this.SoundDescription;
}

/// Play the specified music datablock
function SimObject::playMusic(%this, %music, %volume, %stop)
{
	// Force the previous music to stop?
	%stop = (%stop $= "") ? true : %stop;
	
	// Stop current music
	if (%stop && alxIsPlaying(%this.MusicHandle))
		%this.stopMusic();
	
	// Record the datablock and handle id's
	%this.MusicDescription = %music;
	%this.MusicHandle	   = alxPlay(%music);
	
	// Set the volume
	setHandleVolume(%this.MusicHandle, (%volume $= "") ? 1.0 : %volume);
	
	return %this.MusicHandle;
}

/// Stop the current music
function SimObject::stopMusic(%this)
{
	if (!alxIsPlaying(%this.MusicHandle))
		return;
	
	// Stop the current music
	alxStop(%this.MusicHandle);
}

/// Return the current music's handle
function SimObject::getMusicHandle(%this)
{
	if (!alxIsPlaying(%this.MusicHandle))
		return NULL;
	
	// Return the handle
	return %this.MusicHandle;
}

/// Return the current music's description
function SimObject::getMusicDescription(%this)
{
	if (!alxIsPlaying(%this.MusicHandle))
		return NULL;
	
	// Return the datablock
	return %this.MusicDescription;
}

/// Transition music from the current to the specified
function SimObject::transitionMusic(%this, %newMusic)
{
	// Use the current music handle
	%oldHandle = %this.getMusicHandle();
	
	// Get the interval and the target volume
	%timeInterval = $SoundManager::Transition::Time / $SoundManager::Transition::Itterations;
	%targetVolume = getHandleVolume(%oldHandle);
	
	if (%targetVolume == 0.0)
		%targetVolume = 1.0;
	
	%volumeInterval = %targetVolume / $SoundManager::Transition::Itterations;
	
	// Play the new music and set the volume to zero
	%newHandle = %this.playMusic(%newMusic, false, 0.0);
	
	// Start the transition process
	%this.doTransition(%oldHandle, %newHandle, %timeInterval, %volumeInterval, %targetVolume);
}

/// Perform the transition
function SimObject::doTransition(%this, %oldHandle, %newHandle, %timeInterval, %volumeInterval, %targetVolume)
{
	// Get the new volumes
	%oldVolume = getHandleVolume(%oldHandle) - %volumeInterval;
	%newVolume = %targetVolume - %oldVolume;
	
	// Make sure they don't fall below 0 and get larger than the target
	%oldVolume = mClamp(%oldVolume, 0, %targetVolume);
	%newVolume = mClamp(%newVolume, 0, %targetVolume);
	
	// Set the new volumes
	setHandleVolume(%oldHandle, %oldVolume);
	setHandleVolume(%newHandle, %newVolume);
	
	// If the old handle's volume is now 0 stop transitioning
	if (%oldVolume == 0)
	{
		alxStop(%oldHandle);
		return;
	}
	
	%this.schedule(%timeInterval, "doTransition", %oldHandle, %newHandle, %timeInterval, %volumeInterval, %targetVolume);
}

/// Set the volume on a specified handle
function setHandleVolume(%handle, %volume)
{
	if (!alxIsPlaying(%handle))
		return;
	
	// Set the voume
	alxSourcef(%handle, "AL_GAIN", %volume);
}

/// Return the current volume on the specified handle
function getHandleVolume(%handle)
{
	if (!alxIsPlaying(%handle))
		return 0.0;
	
	// Return the volume
	return alxGetSourcef(%handle, "AL_GAIN");
}

/// Returns whether the handle is looping or not
function getIsLooping(%handle)
{
	if (!alxIsPlaying(%handle))
		return false;
	
	// Returns true if we're looping
	return alxGetSourcei(%handle, "AL_LOOPING");
}