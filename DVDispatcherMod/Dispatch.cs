using UnityEngine;

namespace DVDispatcherMod
{
    public interface IDispatch
    {
        string GetFloatieText(int index);
        Transform GetPointerAt(int index);
        void UpdateDispatch();
    }
}
