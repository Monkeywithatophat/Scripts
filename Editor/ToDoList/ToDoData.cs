using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ToDoData", menuName = "Tools/ToDoList Data")]
public class ToDoData : ScriptableObject
{
    public List<ToDoList.Category> categories = new();
}
