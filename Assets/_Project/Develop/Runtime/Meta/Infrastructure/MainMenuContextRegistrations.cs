using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Meta.Presentation;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuContextRegistrations
    {
        public static void Process(IContainerBuilder builder, MainMenuController controller)
        {
            Debug.Log("Register main menu scene services");
            builder.Register<SceneNavigator>(Lifetime.Scoped);
            // A scoped factory keeps controller.Dispose in the scene-scope teardown.
            builder.Register(GetMainMenuController, Lifetime.Scoped);

            MainMenuController GetMainMenuController(IObjectResolver container) => controller;
        }
    }
}
