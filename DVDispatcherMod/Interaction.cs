using Harmony12;

namespace DVDispatcherMod
{
    class Interaction
    {

    }

    /// <summary>
    /// 
    /// </summary>
    class PageBook_FlipTo_Patch
    {
        [HarmonyPatch(typeof(PageBook), "FlipTo")]
        static void Postfix(int targetPage)
        {

        }
    }
}
