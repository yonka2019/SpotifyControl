# SpotifyControl
Allows easily control spotify volume

## Getting Started
### • CLIENT ID & CLIENT SECRET
First of all, you should fill up the fields CLIENT_ID & CLIENT_SECRET in the App.config.
1. Login into [Spotify Dashboard](https://developer.spotify.com/dashboard)
2. Create a new application with redirect URI: `http://localhost:5543/callback`
3. Copy the CLIENT ID & CLIENT SECRET and fill up into App.config

### • REFRESH TOKEN
Now, we should receive our refresh token in order to request the access token further.

4. Build our application, go to executable file location and run `SpotifyControl r` to update the refresh token (login in the browser and wait until message ' Spotify Authorization was successful.' will be shown)

5. That's it! now you can use the application without any limits.

## Usage
`SpotifyControl +` Increase volume by 10 percent 

`SpotifyControl -` Decrease volume by 10 percent

`SpotifyControl 0` Set volume to 0

You can change the percent which the volume increases/decreases by via changing ***VOLUME_BY*** value (by default is 10 percent)

♧ Don't forget to *rebuild* the application to save changes

## Hide the application
By default, the application runs in *Console Application* mode, if you wish to run the application in "hidden mode" you can modify the `Output type` at project properties to *Windows Application*

***For example, this could be used when binding the volume control to keyboard macro***
