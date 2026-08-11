using EscapeFromNodnarb.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeFromNodnarb.Tests
{
    public sealed class ProjectBootstrapperTests
    {
        [Test]
        public void ConfigureKeepsProceduralEngineTypesAvailable()
        {
            ProjectBootstrapper.Configure();

            Assert.That(PlayerSettings.stripEngineCode, Is.False);
        }

        [Test]
        public void ConfigurePreservesExistingSceneContent()
        {
            ProjectBootstrapper.Configure();
            Scene scene = EditorSceneManager.OpenScene(ProjectBootstrapper.MainScenePath, OpenSceneMode.Single);
            GameObject marker = new GameObject("BootstrapPreservationProbe");
            EditorSceneManager.SaveScene(scene);

            try
            {
                ProjectBootstrapper.Configure();
                Assert.That(GameObject.Find(marker.name), Is.Not.Null);
            }
            finally
            {
                if (marker != null)
                {
                    Object.DestroyImmediate(marker);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
    }
}
