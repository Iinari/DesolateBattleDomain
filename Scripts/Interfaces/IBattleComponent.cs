using UnityEngine;

public interface IBattleComponent
{
    void InitializeNew(BattleData data);
    void InitializeFromData(BattleData data);
}
