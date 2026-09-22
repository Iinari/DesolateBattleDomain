using UnityEngine;

public interface IUIAnchorProvider
{
    bool TryGetAnchor(string key, out Transform anchor);
}
