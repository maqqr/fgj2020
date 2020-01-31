using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Create Settings ScriptableObject", order = 1)]
public class Settings : ScriptableObject
{
    public int tilesHorizontally = 15;
    public int tilesVertically = 10;

}