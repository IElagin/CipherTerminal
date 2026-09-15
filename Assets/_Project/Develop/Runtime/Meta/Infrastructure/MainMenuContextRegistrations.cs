using VContainer;
using UnityEngine;
using Assets._Project.Develop.Runtime.Utilities.SceneManagement;

namespace Assets._Project.Develop.Runtime.Meta.Infrastructure
{
    public class MainMenuContextRegistrations
    {
        public static void Process(IContainerBuilder builder)
        {
            Debug.Log("Register main menu scene services");
            builder.Register<SceneNavigator>(Lifetime.Scoped);
        }
    }
}
