# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.4.0] - 2022-??-?? (not released yet)
### Fixed
 - Fixed that when ```new Texture3D``` causes an exception in the importer, that the Texture3D asset is left in a broken state. Now it will create a magenta Texture3D instead and log an error to the console.
 

## [1.3.0] - 2022-03-11
After installing this update, it will trigger a reimport of all Texture3DAtlas assets in the project and Texture3DAtlas's will no longer be readable via scripts by default.
### Added
 - Added ability to toggle whether the Texture3DAtlas is readable from scripts at the expense of consuming more memory when turned on. The default is off.

### Changed
 - Creating a new Texture3DAtlas asset will now no longer be readable from scripts by default. If you want to restore the previous behavior, you need to enable the ```Read/Write Enabled``` option.


## [1.2.0] - 2021-02-27
### Fixed 
 - Fixed Texture3DAtlas assuming Texture3D support for compressed formats ([Case 1208832](https://issuetracker.unity3d.com/issues/unable-to-create-a-texture3d-with-a-compressed-format)) works in Unity 2019.4. It works in Unity 2020.1 and newer only. Thanks to Richard for pointing this out.
 - Fixed Texture3DAtlas not updating its texture format when changing the build target with [Asset Import Pipeline V2](https://blogs.unity3d.com/2019/10/31/the-new-asset-import-pipeline-solid-foundation-for-speeding-up-asset-imports/) being used. Thanks to Bastien for the help (actually providing the fix/workaround).

### Changed
 - Removed requirement to mark input textures as "Read/Write Enable" due to [Case 1208825](https://issuetracker.unity3d.com/product/unity/issues/guid/1208825). The case has been fixed in Unity 2019.4+, thus no need to enforce this anymore.

### Removed
 - Removed enum member ```Texture3DAtlasImporter.VerifyResult.NotReadable```.

## [1.1.0] - 2020-10-31
### Fixed 
 - Don't display the Texture3DArray imported object twice in the Inspector
 - Texture3DArrays can use texture compression now, due to [Case 1208832](https://issuetracker.unity3d.com/product/unity/issues/guid/1208825) being fixed.
 - In Unity 2020.1 and newer, don't require textures have to be using isReadable anymore, because Graphics.CopyTexture has been fixed https://issuetracker.unity3d.com/product/unity/issues/guid/1208825
 - When running in Unity 2020.1 and newer, use the [new built-in Texture3D preview](https://twitter.com/aras_p/status/1252811959616987138?s=20) when viewing a Texture3D asset in the Inspector. The built-in preview is way nicer and has more features than my implementation for Unity 2019.3.
 - Several minor documentation fixes.

 
## [1.0.0] - 2020-01-19
 - First release