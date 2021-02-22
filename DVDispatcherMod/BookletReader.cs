using DV.Logic.Job;
using DV.RenderTextureSystem.BookletRender;
using Harmony12;
using System.Collections.Generic;

namespace DVDispatcherMod
{
    [HarmonyPatch(typeof(BookletCreator), "GetBookletTemplateData")]
    class BookletCreator_GetBookletTemplateData_Patch
    {
        static void Postfix(ref List<TemplatePaperData> __result, Job job)
        {
            IDispatch[] dispatches = new IDispatch[__result.Count];
            for (int i = 0; i < __result.Count; i++)
            {
                switch (__result[i])
                {
                    case CoverPageTemplatePaperData cptpd:
                        break;
                    case TaskTemplatePaperData ttpd:
                        switch (ttpd.taskType) {
                            case "COUPLE":
                                dispatches[i] = new CoupleDispatch(job, ttpd, false);
                                break;
                            case "UNCOUPLE":
                                dispatches[i] = new CoupleDispatch(job, ttpd, true);
                                break;
                            case "HAUL":
                            case "LOAD":
                            case "UNLOAD":
                            default:
                                dispatches[i] = new NullDispatch();
                                break;
                        }
                        break;
                    case FrontPageTemplatePaperData fptpd:
                    case ValidateJobTaskTemplatePaperData _:
                    default:
                        dispatches[i] = new NullDispatch();
                        break;
                }
            }
        }
    }
}
