# Textured Wireframe Mesh Visualization

## Overview

This technique provides a means to visualize meshes with a texture-based wireframe material without the use of a geometry shader.
In cases where geometry shaders are not available or when an alternative may be desirable, this technique provides similar results
and should be performant in most use cases.

## Usage

- Add the **MeshTexturedWireframeAdapter** script to a GameObject in the scene.
  - Assign the *Meshing Subsystem Component* reference from the scene.
  - Assign the *Wireframe Material* reference to one of the provided wireframe materials.
    - `TexturedWireframeGraphMat`: a Shader Graph based wireframe material
    - `TexturedWireframeHLSLMat`: a simple HLHSL shader based wireframe material
- Set the same *Wireframe Material* used above on to the Mesh prefab (*the MeshRenderer material*)
used by the MeshingSubsystemComponent object.
  - *Note: the MeshTexturedWireframeAdapter won't prepare the mesh for wireframe if it doesn't have this same material.*

The meshes should now appear with the wireframe material when they are added or updated from the MeshingSubsystemComponent.

> #### Material Properties:
> 1. **Line Width**: the wireframe line width in meters (default .001).
Runtime modification example: `wireframeMaterial.SetFloat("_LineWidth", .003f);`
> 2. **High Confidence Line Color**: the line color to use for a high confidence area.
If no confidence data is requested from the MeshingSubsystemComponent, all lines and background will be rendered at high confidence.
> 3. **Low Confidence Line Color**: the line color to use for a low confidence area.
> 4. **High Confidence Background Color**: the background color for a high confidence area.
> 5. **Low Confidence Background Color**: the background color for a low confidence area.


### Copyright

Copyright (c) 2020-present Magic Leap, Inc. All Rights Reserved.
Use of this file is governed by the Developer Agreement, located
here: https://id.magicleap.com/terms/developer
