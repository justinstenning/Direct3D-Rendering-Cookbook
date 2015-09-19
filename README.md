# Direct3D Rendering Cookbook - companion source code
This repository contains the up-to-date companion source code for my book [Direct3D Rendering Cookbook](http://www.amazon.com/gp/product/B00HYQFGYI/ref=as_li_tl?ie=UTF8&camp=1789&creative=9325&creativeASIN=B00HYQFGYI&linkCode=as2&tag=spazzarama03-20&linkId=5CQLVOQWKPW7KDWK) published in January 2014 by [Packt Publishing](https://www.packtpub.com/game-development/direct3d-rendering-cookbook).

![Direct3D Rendering Cookbook cover image](http://ws-na.amazon-adsystem.com/widgets/q?_encoding=UTF8&ASIN=B00HYQFGYI&Format=_SL160_&ID=AsinImage&MarketPlace=US&ServiceVersion=20070822&WS=1&tag=spazzarama03-20)

Any questions or issues can be posted in the [issues](https://github.com/spazzarama/Direct3D-Rendering-Cookbook/issues)

Build Instructions
------------------
To build all projects:
  1. open the relevant solution, 
  2. right-click the root node within the Solution Explorer 
     and select *Rebuild Solution*.

NOTE:
  If for some reason the `MeshContentTask` build target does not
  function correctly for you, the compiled assets are available
  separately from
  [OneDrive](https://onedrive.live.com/?cid=1E4B8ED6FFC56FDC&id=1E4B8ED6FFC56FDC%211800), they
  just need to be copied to the correct build location.

*****************************************

* All desktop recipes (compatible with VS2012 and VS2013):
`.\D3DRendering.sln`

* All Windows Store apps recipes (VS2013 only):
`.\D3DRendering.vs2013.WinRT.sln`

* SharpDX 2.5.1 (beta) and BulletSharp
`.\External`

*****************************************
Shared projects:
-----------------------------------------
* Desktop apps

  `.\Common\Common.csproj`

* Windows Store apps

  `.\Common.WinRT\Common.WinRT.csproj`

*****************************************
Chapter 1: *Getting Started with Direct3D*
-----------------------------------------

* Building a Direct3D 11 application with C# and SharpDX

  `.\Ch01_01EmptyProject\`

* Initializing Direct3D 11.1 device and swap chain

  `.\Ch01_02Direct3D11_1\`

* Debugging your Direct3D application

  `.\Ch01_03Debugging\`

*****************************************
-----------------------------------------
Chapter 2: *Rendering with Direct3D*
-----------------------------------------
* Building a simple rendering framework

  `.\Common\Common.csproj`

* Rendering primitives

  `.\Ch02_01RenderingPrimitives\`

* Adding texture

  `.\Ch02_02AddingTexture\`

*****************************************
Chapter 3: *Rendering Meshes*
-----------------------------------------
* Cube and Sphere

  `.\Ch03_01CubeAndSphere\`

* Material and Lighting

  `.\Ch03_02MaterialAndLighting\`

* Material and Lighting with cube mapping

  `.\Ch03_02WithCubeMapping\`

* Load mesh from file

  `.\Ch03_03LoadMesh\`

*****************************************
Chapter 4: *Animating Meshes with Vertex Skinning*
-----------------------------------------
* Vertex Skinning

  `.\Ch04_01VertexSkinning\`

* Bone Animation

  `.\Ch04_02Animate\`

*****************************************
Chapter 5: *Applying Hardware Tessellation*
-----------------------------------------
* Tessellation basics

  `.\Ch05_01TessellationPrimitives\`

* Tessellation of a mesh

  `.\Ch05_02TessellatedMesh\`

*****************************************
Chapter 6: *Adding Surface Detail with Normal and Displacement Mapping*
-----------------------------------------
* Displacement Mapping

  `.\Ch06_01DisplacementMapping\`

  `.\Ch06_01DisplacementMapping_TangentSpace\`

* Displacement Decals

  `.\Ch06_02DisplacementDecals\`

*****************************************
Chapter 7: *Performing Image Processing Techniques*
-----------------------------------------
* Image processing (compute shaders)

  `.\Ch07_01ImageProcessing\`

*****************************************
Chapter 8: *Incorporating Physics and Simulations*
-----------------------------------------
* Physics (with BulletSharp)

  `.\Ch08_01Physics\`

* Particles (compute shaders with append\consume buffers)

  `.\Ch08_02Particles\`

*****************************************
Chapter 9: *Rendering on Multiple Threads and Deferred Contexts*
-----------------------------------------
* Multithreaded rendering - benchmark

  `.\Ch09_01Benchmark\`

* Multithreaded Dynamic Cube Environment Map

  `.\Ch09_02DynamicCubeMapping\`

* Multithreaded Dual Paraboloid Environment Mapping

  `.\Ch09_03DualParaboloidMapping\`

*****************************************
Chapter 10: *Implementing Deferred Rendering*
-----------------------------------------
* Deferred rendering

  `.\Ch10_01DeferredRendering\`
    * NOTE: the textures for the CryTek Sponza scene can be downloaded
      from the [CryTek CryENGINE 3 downloads page](http://www.crytek.com/cryengine/cryengine3/downloads)
      or [directly here](http://www.crytek.com/download/sponza_textures.rar).
      These should then be placed into the directory: `.\Ch10_01DeferredRendering\textures`

      The project will also compile and render without these textures

*****************************************
Chapter 11: *Integrating Direct3D with XAML and Windows 8.1*
-----------------------------------------
* Direct3D CoreWindow Windows Store app

  `.\Ch11_01HelloCoreWindow\`

* Direct3D SwapChainPanel Windows Store app

  `.\Ch11_02HelloSwapChainPanel\`

* Loading resources asynchronously

  `.\Ch11_03CreatingResourcesAsync\`
    * NOTE: If you do not see the model, *clean and rebuild* the
            project again. This is because the `.cmo` file does 
            not yet exist the first time you build. Cleaning and
            building again will correctly add the model to the 
            Windows Store apps' AppX package.
