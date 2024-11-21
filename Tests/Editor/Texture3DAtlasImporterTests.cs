//
// Texture3D Importer for Unity. Copyright (c) 2019-2024 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture3DAtlasImportPipeline
//
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Oddworm.EditorFramework.Tests
{
    class Texture3DAtlasImporterTests
    {
        /// <summary>
        /// Creates a new Texture3D atlas asset and returns the asset path.
        /// </summary>
        string BeginAssetTest()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + string.Format("Test_Texture3DAtlas.{0}", Texture3DAtlasImporter.kFileExtension));
            System.IO.File.WriteAllText(path, "");
            AssetDatabase.Refresh();
            return path;
        }

        /// <summary>
        /// Deletes the asset specified by path.
        /// </summary>
        /// <param name="path">The path returned by BeginAssetTest().</param>
        void EndAssetTest(string path)
        {
            AssetDatabase.DeleteAsset(path);
        }

        [Test]
        public void DefaultSettings()
        {
            var path = BeginAssetTest();
            try
            {
                var importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);

                Assert.AreEqual(1, importer.anisoLevel);
                Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
                Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
                Assert.AreEqual(0, importer.textures.Length);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void ScriptingAPI_SetProperties()
        {
            var path = BeginAssetTest();
            try
            {
                var anisoLevel = 10;
                var filterMode = FilterMode.Trilinear;
                var wrapMode = TextureWrapMode.Mirror;

                var importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);
                importer.anisoLevel = anisoLevel;
                importer.filterMode = filterMode;
                importer.wrapMode = wrapMode;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                // Reload importer
                importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);

                Assert.AreEqual(anisoLevel, importer.anisoLevel);
                Assert.AreEqual(filterMode, importer.filterMode);
                Assert.AreEqual(wrapMode, importer.wrapMode);
                Assert.AreEqual(0, importer.textures.Length);
            }
            finally
            {
                EndAssetTest(path);
            }
        }


        [Test]
        public void ScriptingAPI_AddMemoryTexture()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);
                var texture = new Texture2D(64, 64, TextureFormat.RGB24, true);

                try
                {
                    importer.textures = new Texture2D[] { texture };
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                finally
                {
                    Texture2D.DestroyImmediate(texture);
                }

                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }


        [Test]
        public void ScriptingAPI_AddNullTexture()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);

                try
                {
                    importer.textures = new Texture2D[] { null };
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void ScriptingAPI_SetNullArray()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);

                try
                {
                    importer.textures = null;
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void LoadTexture3D()
        {
            var path = BeginAssetTest();
            try
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as Texture3D;
                Assert.IsNotNull(asset);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        //[Test]
        public void TextureFormats()
        {
            var cleanup = new List<string>(); // remove these assets afterwards
            var path = BeginAssetTest();
            try
            {
                // These are the paths of our test textures in the package
                var srcPath0 = AssetDatabase.GUIDToAssetPath("e2e8f2c4db9cbae48ad0c09078dbabd5");
                var srcPath1 = AssetDatabase.GUIDToAssetPath("35b624c59e124e6408affa5fef552ae1");
                var srcPath2 = AssetDatabase.GUIDToAssetPath("8670b3720a4f1514ea8438a9f24d96e4");

                // These are the paths where we copy our test textures to
                var dstPath0 = AssetDatabase.GenerateUniqueAssetPath("Assets/" + System.IO.Path.GetFileName(srcPath0));
                var dstPath1 = AssetDatabase.GenerateUniqueAssetPath("Assets/" + System.IO.Path.GetFileName(srcPath1));
                var dstPath2 = AssetDatabase.GenerateUniqueAssetPath("Assets/" + System.IO.Path.GetFileName(srcPath2));

                // Make sure to remove the test assets afterwards
                cleanup.Add(dstPath0);
                cleanup.Add(dstPath1);
                cleanup.Add(dstPath2);

                // Copy test assets
                FileUtil.CopyFileOrDirectory(srcPath0, dstPath0);
                FileUtil.CopyFileOrDirectory(srcPath1, dstPath1);
                FileUtil.CopyFileOrDirectory(srcPath2, dstPath2);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                // Assign test textures to test 3d texture
                var importerTex3d = (Texture3DAtlasImporter)AssetImporter.GetAtPath(path);
                importerTex3d.textures = new Texture2D[]
                {
                    AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath0),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath1),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath2)
                };
                importerTex3d.SaveAndReimport();

                // Use each texture format
                var formats = GetSupportedTextureImporterFormats();
                foreach(var format in formats)
                {
                    // Set the Texture2D's to the corresponding texture format
                    AssetDatabase.StartAssetEditing();
                    try
                    {
                        foreach (var destPath in new[] { dstPath0, dstPath1, dstPath2 })
                        {
                            var importer = (TextureImporter)AssetImporter.GetAtPath(destPath);
                            //importer.sRGBTexture = true;

                            var settings = importer.GetDefaultPlatformTextureSettings();
                            settings.overridden = true;
                            settings.format = format;
                            importer.SetPlatformTextureSettings(settings);

                            importer.SaveAndReimport();
                        }

                        AssetDatabase.SaveAssets();
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                    }

                    // Now check i
                    var tex0 = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath0);
                    var tex1 = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath1);
                    var tex2 = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath2);

                    var tex3d = AssetDatabase.LoadAssetAtPath<Texture3D>(path);
                    var tex2d = new Texture2D(tex3d.width, tex3d.height, tex3d.format, tex3d.mipmapCount > 1);

                    Graphics.CopyTexture(tex3d, 0, tex2d, 0);
                    System.IO.File.WriteAllBytes("Assets/Slice0.png", tex2d.EncodeToPNG());

                    Graphics.CopyTexture(tex3d, 1, tex2d, 0);
                    System.IO.File.WriteAllBytes("Assets/Slice1.png", tex2d.EncodeToPNG());

                    Graphics.CopyTexture(tex3d, 2, tex2d, 0);
                    System.IO.File.WriteAllBytes("Assets/Slice2.png", tex2d.EncodeToPNG());

                    Debug.Log(tex3d.format);
                }
            }
            finally
            {
                foreach(var p in cleanup)
                {
                    if (System.IO.File.Exists(p))
                        AssetDatabase.DeleteAsset(p);
                }

                EndAssetTest(path);

                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Gets all supported texture importer formats for the active build target.
        /// </summary>
        /// <returns></returns>
        List<TextureImporterFormat> GetSupportedTextureImporterFormats()
        {
            var formats = new List<TextureImporterFormat>();

            formats.Add(TextureImporterFormat.RGBA32);
            //return formats;

            foreach (var f in System.Enum.GetValues(typeof(TextureImporterFormat)))
            {
                var format = (TextureImporterFormat)f;
                if (TextureImporter.IsPlatformTextureFormatValid(TextureImporterType.Default, EditorUserBuildSettings.activeBuildTarget, format))
                    formats.Add(format);
            }

            return formats;
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        //[UnityTest]
        //public IEnumerator Texture2DArrayImporterTestWithEnumeratorPasses()
        //{
        //    // Use the Assert class to test conditions.
        //    // Use yield to skip a frame.
        //    yield return null;
        //}
    }
}
