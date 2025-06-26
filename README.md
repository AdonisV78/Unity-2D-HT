This is a heat transfer simulation I created. It is very basic in what it can do, but still helpful in showing how different factors affect heat transfer, and it is also fun to just mess around with.
It is very simple, 2-dimensional heat transfer using the top, bottom, left, and right sides of heat cells. There is functionality for convective, radiative, and conductive heat transfer. There is also a PDF included for various material and fluid properties at certain temperatures. Using this allows for a more realistic depiction of what would happen. Some bugs can happen, notably when messing with settings in run time, or having the heat transfer happen too quickly (commonly do to having squares that are too small)

To use/run this project, you should download Unity, the files for this project, extract the files, and then open the project in Unity. From there, all of the settings for the simulation can be changed in the inspector, and you will have access to the code if you'd like to take a better look or change anything.

I tried to make all the variable names clear and add explanations of what they are when the names are hovered over. All units of the variables are displayed when hovered over, and I have tried to make it so that the code is well-commented, so that it is easy to follow along.

The standard HT2D script is designed for visualizing actual numbers and heat transfer on a smaller scale, whereas the optimized HT2D script is intended for viewing how it would appear in the real world.
This is because the optimized script edits the colors on a texture instead of game objects, so many more points (cells in the script) can be rendered, creating a higher resolution effect.

If you find this project useful and want to expand upon it or use parts of it for anything you are working on, feel free to as long as I am properly credited.

I may come back to this project in the future to expand the functionality, maybe adding the option to use different shapes, like a circle, or possibly having the option to use heat exchangers.
In the meantime, I hope you enjoy using this, and thank you for taking the time to check it out.
