// Isolated tests of the production talent logic and event callbacks.
// Stubs model the API boundary; these do not claim to test Harmony or NetCode in-game.
using System;
using System.Collections.Generic;
using GoldenChance.Patches;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }
}
namespace UnityEngine
{
    public static class Mathf { public static int Clamp(int x, int min, int max) => Math.Min(max, Math.Max(min, x)); }
}
namespace GoldenChance
{
    public static class GoldenChanceMod { public static int PlantBonus, CookingBonus; }
}
public enum ConditionID { Other, ChanceToGainRarePlant, ChanceForExtraCookedFoodToBeRare }
public struct ConditionData { public ConditionID conditionID; public int value; }
public sealed class Commands
{
    public readonly List<ConditionData> Sent = new List<ConditionData>();
    public void SetSkillTalentCondition(int entity, ConditionData value) => Sent.Add(value);
}
public sealed class EntityManager { public bool Present = true; public bool Exists(int entity) => Present; }
public sealed class World { public bool IsCreated = true; public EntityManager EntityManager = new EntityManager(); }
public sealed class PlayerController
{
    public bool isLocal = true;
    public int entity = 1;
    public World world = new World();
    public Commands playerCommandSystem = new Commands();
}
public sealed class SaveManager
{
    public Dictionary<int, List<int>> Points = new Dictionary<int, List<int>>();
    public List<int> GetSkillTalentTreesPoints(int id) => Points.TryGetValue(id, out var points) ? points : null;
    public void SetSkillTalentPoint() { }
    public void ResetTalentTree() { }
}
public sealed class SkillTalentsTable
{
    public sealed class Talent { public ConditionID givesCondition; public int Base, PerPoint; }
    public sealed class Tree { public int skillID; public List<Talent> skillTalents = new List<Talent>(); }
    public List<Tree> skillTalentTrees = new List<Tree>();
    public ConditionData GetConditionDataForSkillTalent(int id, int index, int points)
    {
        var talent = skillTalentTrees.Find(t => t.skillID == id).skillTalents[index];
        var data = new ConditionData { conditionID = talent.givesCondition, value = talent.Base + talent.PerPoint * points };
        // Simulate the existing native getter's Harmony postfix.
        TalentValuePatch.Postfix(ref data);
        return data;
    }
}
public static class Manager
{
    public sealed class Main { public PlayerController player = new PlayerController(); }
    public sealed class Mod { public SkillTalentsTable SkillTalentsTable = new SkillTalentsTable(); }
    public static Main main = new Main();
    public static Mod mod = new Mod();
    public static SaveManager saves = new SaveManager();
}
public static class TalentEventTests
{
    private static int checks;
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static void Expected(int plant, int food, string label)
    {
        var sent = Manager.main.player.playerCommandSystem.Sent;
        Check(sent.Count == 2, label + ": sends only two golden conditions");
        Check(sent[0].value == plant && sent[1].value == food, label + ": values");
        sent.Clear();
    }
    public static void Main()
    {
        var table = Manager.mod.SkillTalentsTable;
        table.skillTalentTrees.Add(new SkillTalentsTable.Tree { skillID = 1, skillTalents = new List<SkillTalentsTable.Talent> {
            new SkillTalentsTable.Talent { givesCondition = ConditionID.Other, Base = 50 },
            new SkillTalentsTable.Talent { givesCondition = ConditionID.ChanceToGainRarePlant, Base = 3, PerPoint = 3 } } });
        table.skillTalentTrees.Add(new SkillTalentsTable.Tree { skillID = 2, skillTalents = new List<SkillTalentsTable.Talent> {
            new SkillTalentsTable.Talent { givesCondition = ConditionID.ChanceForExtraCookedFoodToBeRare, PerPoint = 5 } } });
        var player = Manager.main.player;
        GoldenChance.GoldenChanceMod.PlantBonus = 20;
        GoldenChance.GoldenChanceMod.CookingBonus = 30;
        TalentJoinPatch.Postfix(player);
        Expected(23, 30, "join without allocated points");
        Manager.saves.Points[1] = new List<int> { 0, 5 };
        Manager.saves.Points[2] = new List<int> { 5 };
        TalentPointChangedPatch.Postfix();
        Expected(38, 55, "talent changes");
        TalentPointChangedPatch.Postfix();
        Expected(38, 55, "repeat event never compounds bonuses");
        Manager.saves.Points[1].Clear(); Manager.saves.Points[2].Clear();
        TalentResetConfirmedPatch.Postfix();
        Expected(23, 30, "confirmed reset with cleared point lists");
        TalentResetConfirmedPatch.Postfix();
        Expected(23, 30, "reset when already at zero");
        GoldenChance.GoldenChanceMod.PlantBonus = 100; GoldenChance.GoldenChanceMod.CookingBonus = 100;
        TalentValuePatch.ResendTalentValues();
        Expected(100, 100, "live settings cap at 100");
        GoldenChance.GoldenChanceMod.PlantBonus = 0; GoldenChance.GoldenChanceMod.CookingBonus = 0;
        TalentValuePatch.ResendTalentValues();
        Expected(3, 0, "return to vanilla settings");
        var other = new ConditionData { conditionID = ConditionID.Other, value = 57 };
        TalentValuePatch.Postfix(ref other); Check(other.value == 57, "unrelated condition untouched");
        player.isLocal = false; TalentJoinPatch.Postfix(player); TalentPointChangedPatch.Postfix();
        Check(player.playerCommandSystem.Sent.Count == 0, "remote players do not use host points");
        player.isLocal = true; player.world.EntityManager.Present = false; TalentValuePatch.ResendTalentValues();
        Check(player.playerCommandSystem.Sent.Count == 0, "destroyed entity ignored");
        player.world.EntityManager.Present = true; player.world.IsCreated = false; TalentValuePatch.ResendTalentValues();
        Check(player.playerCommandSystem.Sent.Count == 0, "closed world ignored");
        player.world.IsCreated = true; player.playerCommandSystem = null; TalentValuePatch.ResendTalentValues(); checks++;
        Manager.main.player = null; TalentValuePatch.ResendTalentValues(); checks++;
        Console.WriteLine(checks + " talent-event checks passed.");
    }
}
