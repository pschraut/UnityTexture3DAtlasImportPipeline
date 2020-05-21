# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2020-??-??
### Fixed 
 - In Unity 2020.1 and newer, don't require textures have to be using isReadable anymore, because Graphics.CopyTexture has been fixed https://issuetracker.unity3d.com/product/unity/issues/guid/1208825
 - When running in Unity 2020.1 and newer, use the [new built-in Texture3D preview](https://twitter.com/aras_p/status/1252811959616987138?s=20) when viewing a Texture3D asset in the Inspector. The built-in preview is way nicer and has more features than my implementation for Unity 2019.3.
 - Several minor documentation fixes.
 
 
## [1.0.0] - 2020-01-19
 - First release