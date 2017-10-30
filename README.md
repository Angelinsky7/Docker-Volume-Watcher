# Docker-Volume-Watcher
Docker volume watcher is a service that creates a link between window file system and docker mounted volume

This is an adaptation of this project : https://github.com/merofeev/docker-windows-volume-watcher but without the need to have python installed.

Everything is automatically handle by the service who watches all mounted docker volumes and create a file watcher wrapper (https://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher(v=vs.110).aspx) arround it.

When the file changes, the service send a custom exec command to each container to update the current permission of the file (without changing it).

## Starting the service
Start the application by executing the shortcut on the desktop or in the start menu
A tray application should appears near the clock
Be aware that directly starting the service in the services manager leads to the service "crash". This is because for the service to start it need some parameters (polling interval and dvwignore file activation) so you must start it from the tray app. (a little bit like the Docker service)

## Managing
Right clicking the tray app icon leads to an context menu where you can open the settings window.

### General
Panel to configure general options
1. Start at login
2. Check upate

### Properties
Panel to configure service properties
1. Polling interval of the Docker Volume Watcher for the conainters to the Docker service in millisecond
2. Only watch containers that have a dvwingore file at the root of the source path. Like that it can protect from slowing down containers that you don't need to observe.
For example, if you use docker-compose to build you environment and have multiple containers like :
Apache - Frontend (no need to listen to file changes there)
Mysql - Database (no need to listen to file changes there)
Node - Typescript (for example) project (only some file should be observed)
3. Type of shell (and how) used by the notifier (sh first then bash, bash first then sh, only bash, only sh)
4. Docker endpoint (by default: "npipe://./pipe/docker_engine")

### Reset
Panel to restart the service
1. Restart the service

## Informations
You can find informations of what the Service is doing into the Window Event Viewer window.
Go to "Applications and Services logs" -> "Docker Volume Watcher"

## .dvwignore file
if you add a .dvwignore file at the root of the host volume you can now ignore some files

the syntax is :

```
## Ignore Docker volume Watcher

# Node.js Tools for Visual Studio
node_modules/*
.npm/*
*.zip
dir/*/file.zip
dir/*.zip
```
