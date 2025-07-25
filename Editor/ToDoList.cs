#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ToDoList : EditorWindow
{
    public enum TaskStatus { NotDone, WorkingOn, Done }
    private ToDoData data;

    [System.Serializable]
    public class Task
    {
        public string description;
        public TaskStatus status;
    }

    [System.Serializable]
    public class Category
    {
        public string name;
        public List<Task> tasks = new();
        public bool isExpanded = false;
        [System.NonSerialized] public string newTask = "";
    }


    [System.Serializable]
    private class CategoryListWrapper
    {
        public List<Category> categories = new();
    }

    private List<Category> categories = new();
    private string newCategoryName = "";

    private string newTask = "";
    private Vector2 scroll;

    private Task draggingTask;
    private Vector2 dragOffset;
    private Dictionary<TaskStatus, Rect> dropZones = new();

    private const string PrefsKey = "ToDoList_Json";

    private class ConfettiParticle
    {
        public Vector2 position;
        public Vector2 velocity;
        public Color color;
        public float lifetime;
        public float rotation;
        public float size;
    }

    private class SmileyParticle
    {
        public Vector2 position;
        public Vector2 velocity;
        public float lifetime;
        public float size;
        public float rotation;
    }

    private List<ConfettiParticle> confettiParticles = new();
    private List<SmileyParticle> smileyParticles = new List<SmileyParticle>();
    private Vector2? lastDropPosition;

    private float smileyTimer = 0f;
    private const float smileyDuration = 1.5f;


    [MenuItem("TopHats Shit/Tools/To Do List")]
    public static void ShowWindow()
    {
        ToDoList window = GetWindow<ToDoList>("To Do List");
        window.LoadData();
    }

    private void LoadData()
    {
        string[] guids = AssetDatabase.FindAssets("t:ToDoData");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            data = AssetDatabase.LoadAssetAtPath<ToDoData>(path);
        }

        if (data == null)
        {
            data = CreateInstance<ToDoData>();
            AssetDatabase.CreateAsset(data, "Assets/ToDoData.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        categories = data.categories;
    }

    private void OnEnable()
    {
        LoadTasks();
        lastUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateEffects;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateEffects;
    }

    private void SaveTasks()
    {
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    private void LoadTasks()
    {
        string json = EditorPrefs.GetString(PrefsKey, "{}");
        CategoryListWrapper wrapper = JsonUtility.FromJson<CategoryListWrapper>(json);
        categories = wrapper?.categories ?? new List<Category>();
    }

    private void OnGUI()
    {
        GUILayout.Label("Add Category", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        newCategoryName = EditorGUILayout.TextField(newCategoryName);
        if (GUILayout.Button("Add Category", GUILayout.Width(100)))
        {
            if (!string.IsNullOrWhiteSpace(newCategoryName))
            {
                categories.Add(new Category { name = newCategoryName });
                newCategoryName = "";
                SaveTasks();
            }
        }
        GUILayout.EndHorizontal();

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (var category in categories.ToList())
        {
            DrawCategory(category);
        }

        GUILayout.EndScrollView();
        HandleDrag();
        DrawConfetti();
        DrawSmileys();
    }

    private void DrawCategory(Category category)
    {
        GUILayout.Space(10);
        Rect headerRect = EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label(category.name, EditorStyles.boldLabel);
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            categories.Remove(category);
            SaveTasks();
            EditorGUILayout.EndHorizontal();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && headerRect.Contains(Event.current.mousePosition))
        {
            category.isExpanded = !category.isExpanded;
            Event.current.Use();
        }

        if (!category.isExpanded) return;

        GUILayout.BeginHorizontal();
        GUILayout.Space(20);
        GUILayout.BeginVertical();

        GUILayout.Label("Add Task");
        category.newTask = EditorGUILayout.TextField(category.newTask);
        if (GUILayout.Button("Add Task", GUILayout.Width(80)))
        {
            if (!string.IsNullOrWhiteSpace(category.newTask))
            {
                category.tasks.Add(new Task { description = category.newTask, status = TaskStatus.NotDone });
                category.newTask = "";
                SaveTasks();
            }
        }

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        DrawColumn(category, TaskStatus.NotDone, "Not Done");
        DrawColumn(category, TaskStatus.WorkingOn, "Working On");
        DrawColumn(category, TaskStatus.Done, "Done");
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DrawColumn(Category category, TaskStatus status, string label)
    {
        GUILayout.BeginVertical(GUILayout.Width(position.width / 3 - 20));
        GUILayout.Label(label, EditorStyles.boldLabel);

        Rect colStart = GUILayoutUtility.GetLastRect();
        float yStart = colStart.yMax + 5;
        Rect dropRect = new Rect(colStart.x, yStart, position.width / 3 - 10, position.height);
        dropZones[status] = dropRect;

        foreach (var task in category.tasks.Where(t => t.status == status).ToList())
        {
            Rect taskRect = EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(task.description, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                category.tasks.Remove(task);
                SaveTasks();
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (taskRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    draggingTask = task;
                    dragOffset = Event.current.mousePosition - taskRect.position;
                    Event.current.Use();
                }
            }
        }

        GUILayout.EndVertical();
    }

    private void HandleDrag()
    {
        if (draggingTask != null)
        {
            GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 200, 25),
                      draggingTask.description, EditorStyles.helpBox);
            Repaint();

            if (Event.current.type == EventType.MouseUp)
            {
                foreach (var zone in dropZones)
                {
                    if (zone.Value.Contains(Event.current.mousePosition))
                    {
                        draggingTask.status = zone.Key;
                        lastDropPosition = Event.current.mousePosition;

                        if (zone.Key == TaskStatus.Done)
                        {
                            SpawnConfetti(lastDropPosition.Value);
                        }
                        else if (zone.Key == TaskStatus.WorkingOn)
                        {
                            SpawnSmileys(lastDropPosition.Value);
                        }

                        SaveTasks();
                        break;
                    }
                }

                draggingTask = null;
                Event.current.Use();
            }
        }
    }

    private void SpawnConfetti(Vector2 position)
    {
        confettiParticles.Clear();

        Color[] brightColors = new Color[]
        {
            Color.red,
            Color.yellow,
            Color.blue,
            Color.green,
            Color.cyan,
            Color.magenta,
            new Color(1f, 0.5f, 0f),
            new Color(1f, 0f, 0.5f),
        };

        for (int i = 0; i < 30; i++)
        {
            confettiParticles.Add(new ConfettiParticle
            {
                position = position,
                velocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-3f, -1f)),
                color = brightColors[UnityEngine.Random.Range(0, brightColors.Length)],
                lifetime = 1.4f,
                rotation = UnityEngine.Random.Range(0f, 360f),
                size = UnityEngine.Random.Range(6f, 12f)
            });
        }
    }

    private void SpawnSmileys(Vector2 position)
    {
        smileyParticles.Clear();

        for (int i = 0; i < 20; i++)
        {
            smileyParticles.Add(new SmileyParticle
            {
                position = position,
                velocity = new Vector2(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-4f, -1f)),
                lifetime = 1.2f,
                size = UnityEngine.Random.Range(18f, 32f),
                rotation = UnityEngine.Random.Range(0f, 360f)
            });
        }
    }

    private double lastUpdateTime = 0;

    private void UpdateEffects()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        float delta = (float)(currentTime - lastUpdateTime);
        lastUpdateTime = currentTime;
        bool needsRepaint = false;
        for (int i = confettiParticles.Count - 1; i >= 0; i--)
        {
            var p = confettiParticles[i];
            p.velocity += new Vector2(0, 0.1f);
            p.position += p.velocity;
            p.rotation += UnityEngine.Random.Range(-2f, 2f);
            p.lifetime -= delta;

            if (p.lifetime <= 0)
                confettiParticles.RemoveAt(i);
            else
                needsRepaint = true;
        }

        for (int i = smileyParticles.Count - 1; i >= 0; i--)
        {
            var p = smileyParticles[i];
            p.velocity += new Vector2(0, 0.15f);
            p.position += p.velocity;
            p.rotation += UnityEngine.Random.Range(-5f, 5f);
            p.lifetime -= delta;

            if (p.lifetime <= 0)
                smileyParticles.RemoveAt(i);
            else
                needsRepaint = true;
        }

        if (needsRepaint)
            Repaint();
    }

    private void DrawConfetti()
    {
        foreach (var p in confettiParticles)
        {
            Matrix4x4 prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(p.rotation, p.position);
            Color prevColor = GUI.color;
            GUI.color = p.color;
            GUI.DrawTexture(new Rect(p.position.x, p.position.y, p.size, p.size), Texture2D.whiteTexture);
            GUI.color = prevColor;
            GUI.matrix = prev;
        }
    }

    private void DrawSmileys()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter
        };

        foreach (var p in smileyParticles)
        {
            Color prevColor = GUI.color;
            float alpha = Mathf.Clamp01(p.lifetime / 1.2f);
            GUI.color = new Color(1f, 1f, 0f, alpha);
            Matrix4x4 prevMatrix = GUI.matrix;

            GUIUtility.RotateAroundPivot(p.rotation, p.position);
            Rect rect = new Rect(p.position.x - p.size / 2, p.position.y - p.size / 2, p.size, p.size);
            GUI.Label(rect, "☺", style);

            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }
    }
}
#endif
