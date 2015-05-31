caelum4csharp
=============

This is a port of the [Caelum sky project for Ogre](http://www.ogre3d.org/tikiwiki/Caelum), targeting [opengl4charp](https://github.com/giawa/opengl4csharp).  Some functionality has been lost, while some has been added.

Have you ever wanted to easily add a skydome to your game written in C#?  Well, now you can!  Fork, clone and enjoy this repository, and don't forget to thank the original authors of Caelum.

## License
Check the included [LICENSE.md](https://github.com/giawa/caelum4csharp/blob/master/LICENSE) file for the license associated with this code.

## Building the Project
The project includes a .sln and 2 .csproj files which will create an OpenGL program and a class library.  Both the .sln and .csproj are compatible with Visual Studio 2013 and later.  All of the associated .dlls (such as Tao.FreeGlut and OpenGL) are included in the libs folder of each project.  The textures for Caelum are included in the libs folder as well, and need to be included in the bin folder after compilation.

## Screenshots
![Screenshot 1](https://giawa.github.com/caelum/screenshot1.png)

![Screenshot 2](https://giawa.github.com/caelum/screenshot2.png)

![Screenshot 3](https://giawa.github.com/caelum/screenshot3.png)

![Screenshot 4](https://giawa.github.com/caelum/screenshot4.png)

![Screenshot 5](https://giawa.github.com/caelum/screenshot5.png)

![Screenshot 6](https://giawa.github.com/caelum/screenshot6.png)

## TODO
1. Currently the stars move based on a static rotation unaffected by latitude/longitude.
2. Make it a bit easier to deal with ICameraBound stuff.
3. Documentation!