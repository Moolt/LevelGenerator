# Level Generator

Hi there,
I've developed a plug-in for Unity  that basically enables you to click together procedurally generated indoor levels. It works by creating chunks which are basically templates of rooms with randomized properties and contents.
The idea of the plug-in is to visualize the process of procedural content generation, as it get's more complicated the more complex your levels are. In order to make PCG less of a black box and more accessible for level designers, all the logic responsible for generation of meshes or other randomized properties are encapsulated in so called abstract components, which are basically ordinary unity components which will only have definite values once the level has been generated. Abstract components are the interface between level designers, who use the components for actual level design, and programmers, who can create additional components depending on the project. The video below explains all components currently included in the plug-in and shows the generation of whole levels:

# Demo
[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/P2rAnXnNdSI/0.jpg)](https://www.youtube.com/watch?v=P2rAnXnNdSI)

# Chunks

The plug-in's core are chunks which are used at runtime to instantiate actual rooms. Chunks contain abstract components in order to randomize its content. The rooms are then connected by corridors to form whole levels. Rooms created from the same chunk will always be similar but not identical. Here are some example for chunks and their instances:

![alt text](https://i.imgur.com/nq30sC6.jpg)

# User Interface

The GUI is quite simple. It consists mostly of three editor windows for the creation of chunks, abstract components and levels:

![alt text](https://i.imgur.com/BV8hWwG.jpg)

For now I consider the plug-in completed, even though it's still a bit buggy. Depending on the demand I might continue development by fixing a few bugs or adding simple features.

You can find the plug-in on my Github: https://github.com/Moolt/LevelGenerator/tree/master/
Or the whole project as zip file: https://github.com/Moolt/LevelGenerator/archive/master.zip

I hope anyone has any uses for my plug-in. Besides from creating whole games with it, it can also be very handy for prototyping FPS games. Use it to your hearts desire, you may also use it in commercial projects ~~if you give me credits.~~ ~~Actually it's fine if you don't credit me.~~ __The project is now under the MIT license.__
