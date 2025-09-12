#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

// ———————————————————————————————————————————————————————
// EnemyCount drawer: show “Enemy Type #” instead of Element #
// ———————————————————————————————————————————————————————
[CustomPropertyDrawer(typeof(EnemyCount))]
public class EnemyCountDrawer : PropertyDrawer
{
    static string[]  _names;
    static EnemySO[] _enemies;

    void Init()
    {
        var guids = AssetDatabase.FindAssets("t:EnemySO");
        _enemies = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<EnemySO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(e => e != null)
            .ToArray();
        _names = _enemies.Select(e => e.name).ToArray();
    }

    public override void OnGUI(Rect r, SerializedProperty prop, GUIContent label)
    {
        if (_names == null) Init();

        // figure out which element index this is:
        var path   = prop.propertyPath;  
        var matches= Regex.Matches(path, @"Array\.data\[(\d+)\]");
        int  idx   = matches.Count > 0
                    ? int.Parse(matches[matches.Count - 1].Groups[1].Value)
                    : 0;

        // enemy dropdown
        var enemyProp = prop.FindPropertyRelative("enemy");
        int  sel      = System.Array.IndexOf(_enemies, enemyProp.objectReferenceValue as EnemySO);
        string labelText = $"Enemy Type {idx+1}";
        int  newSel   = EditorGUI.Popup(
            new Rect(r.x, r.y, r.width * 0.6f, EditorGUIUtility.singleLineHeight),
            labelText, 
            sel >= 0 ? sel : 0, 
            _names
        );
        enemyProp.objectReferenceValue = _enemies[newSel];

        // count field
        var countProp = prop.FindPropertyRelative("count");
        EditorGUI.PropertyField(
            new Rect(r.x + r.width * 0.62f, r.y, r.width * 0.38f, EditorGUIUtility.singleLineHeight),
            countProp,
            GUIContent.none
        );
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        => EditorGUIUtility.singleLineHeight;
}

// ———————————————————————————————————————————————————————
// CardSO drawer: show “Card #” instead of Element #
// ———————————————————————————————————————————————————————
[CustomPropertyDrawer(typeof(CardSO), true)]
public class CardSOPopupDrawer : PropertyDrawer
{
    static string[] _names;
    static CardSO[] _cards;

    void Init()
    {
        var guids = AssetDatabase.FindAssets("t:CardSO");
        _cards = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<CardSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null)
            .ToArray();
        _names = _cards.Select(c => c.name).ToArray();
    }

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        if (_names == null) Init();

        // get index in list
        var path = prop.propertyPath;
        var matches = Regex.Matches(path, @"Array\.data\[(\d+)\]");
        int idx = matches.Count > 0
                     ? int.Parse(matches[matches.Count - 1].Groups[1].Value)
                     : 0;

        // dropdown
        int sel = System.Array.IndexOf(_cards, prop.objectReferenceValue as CardSO);
        string lbl = $"Card {idx + 1}";
        int newSel = EditorGUI.Popup(pos, lbl, sel >= 0 ? sel : 0, _names);
        prop.objectReferenceValue = _cards[newSel];
    }
}


// Draws each Wave in a List<Wave> with a label "Wave N" instead of "Element N"
[CustomPropertyDrawer(typeof(Wave), true)]
public class WaveDrawer : PropertyDrawer
{
    static readonly Regex _rgx = new Regex(@"Array\.data\[(\d+)\]$");

    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        // extract the index from the property path
        int idx = 0;
        var path = prop.propertyPath;
        var m = _rgx.Match(path);
        if (m.Success)
            idx = int.Parse(m.Groups[1].Value);

        // custom label
        var waveLabel = new GUIContent($"Wave {idx + 1}");
        EditorGUI.PropertyField(pos, prop, waveLabel, true);
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
        // ensure child properties are accounted for
        return EditorGUI.GetPropertyHeight(prop, label, true);
    }
}
#endif

