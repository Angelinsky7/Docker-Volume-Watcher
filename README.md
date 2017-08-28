# Docker-Volume-Watcher
Docker volum watcher is a service that creates a link between window file system and docker mounted volume

This is an adaptation of this project : https://github.com/merofeev/docker-windows-volume-watcher but without the need to have python installed.

Everything is automatically handle by the service who watches all mounted docker volumes and create a file watcher wrapper (https://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher(v=vs.110).aspx) arround it.

When the file changes, the service send a custom exec command to each container to update the current permission of the file (without changing it).

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
