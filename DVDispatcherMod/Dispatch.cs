using UnityEngine;

namespace DVDispatcherMod
{
    public interface IDispatch
    {
        string GetFloatieText(int index);
        Transform GetPointerAt(int index);
        void UpdateDispatch();
    }

    public class NullDispatch : IDispatch
    {
        string IDispatch.GetFloatieText(int index)
        {
            return null;
        }

        Transform IDispatch.GetPointerAt(int index)
        {
            return null;
        }

        void IDispatch.UpdateDispatch()
        {
        }
    }
}
