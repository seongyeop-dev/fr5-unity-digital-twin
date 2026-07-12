using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Keeps display-only TMP text localized after existing runtime panels refresh their values.
/// </summary>
[DisallowMultipleComponent]
public sealed class scr_FR5KoreanUIRuntimeLocalizer : MonoBehaviour
{
    private readonly List<TMP_Text> sceneTexts = new List<TMP_Text>();
    private readonly Dictionary<int, string> observedValues = new Dictionary<int, string>();

    private void Awake()
    {
        RefreshTargets();
        LocalizeChangedText();
    }

    private void OnEnable()
    {
        RefreshTargets();
        LocalizeChangedText();
    }

    private void LateUpdate()
    {
        LocalizeChangedText();
    }

    private void RefreshTargets()
    {
        sceneTexts.Clear();
        observedValues.Clear();

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.scene == gameObject.scene)
            {
                sceneTexts.Add(texts[i]);
            }
        }
    }

    private void LocalizeChangedText()
    {
        for (int i = 0; i < sceneTexts.Count; i++)
        {
            TMP_Text text = sceneTexts[i];
            if (text == null)
            {
                continue;
            }

            int id = text.GetInstanceID();
            string current = text.text;
            string observed;
            if (observedValues.TryGetValue(id, out observed) && observed == current)
            {
                continue;
            }

            string localized = scr_FR5KoreanDisplayText.Localize(current);
            if (localized != current)
            {
                text.text = localized;
            }

            observedValues[id] = localized;
        }
    }
}
