## MeshPicking

## What is it?
Mesh picking is a feature that allows you to get gameObjects through a render pass.
It allows you to have a high level of accuracy without using space partioning, collision and ray cast.
When you call the Picking function, the mesh visible with the Mesh picking script is drawn in a specific render pass and receives the desired pixel at a position to know the ID of the selected object.

## How to install:
Add the PickingSystem component to a gameObject and right click on it. Then click on Install. The installation will add the picking layer and shader to your default compiled shader.


## How to use:
Add the mesh picking component to your gameObject and call the PickingSystem.Instance.Picking function to process the picking and get the selected gameObject.
Only the gameObject with the mesh picking component can be selected.
If you want to improve the accuracy or optimize the picking process, you can manually add the PickingSystem component to a gameObject. Inside this component you can define the size of your render buffer according to the screen, its channel, the precision of the Z-buffer...
The only thing you need to use qre the MeshPicking and PickingSystem scripts. The picking shaders and rendering pipeline are used internally.

## Technical aspect:
This tool has been tested qnd build on an Android and Windows application.
Each time you want to select an element, a new rendering buffer is created and is placed in the temporary buffer of Unity.
This allows to simplify the code and to modify the renderTexture specification without recreating it.
A custum render pipeline is used. If you use a GPU debugger like renderDoc, you can see that this pass is as simple as possible.
The main camera, materials, and layers of the gameObjects are modified for the picking pass and reset afterwards.

## License:
This tool is based on the unity license and the MIT license. So you are free to modify this tool according to unity rules.

## Bug or problem report:
Feel free to report your problem or bug in my mail below. Please be consistent in your description to reproduce the bug in my machine.

## Author:
Six Jonathan
Email: Six-Jonathan@orange.fr

Translated with www.DeepL.com/Translator (free version)