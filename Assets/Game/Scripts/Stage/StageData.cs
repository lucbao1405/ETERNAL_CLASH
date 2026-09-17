using System;
using System.Collections.Generic;

[Serializable]
public class StageData
{
    public string stageId;
    public List<EncounterData> encounters = new();
    public int rewardGold;
}

[Serializable]
public class EncounterData
{
    public List<EnemySpawnData> enemies = new();
}

[Serializable]
public class EnemySpawnData
{
    public string enemyId;
    public int amount;
}
