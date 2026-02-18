// AwakeningDatabaseSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Awakening/Awakening Database")]
public sealed class AwakeningDatabaseSO : ScriptableObject
{
    [SerializeField] private AwakeningRecipeSO[] recipes;
    public AwakeningRecipeSO[] Recipes => recipes;
}