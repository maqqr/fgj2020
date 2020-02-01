using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameInfoController : MonoBehaviour
{
    [SerializeField]
    private Transform infoPanel = null;
    [SerializeField]
    private Settings s = null;

    private Dictionary<int, TextMeshProUGUI> unitCounts = new Dictionary<int, TextMeshProUGUI>();

    public void AddPlayerInfo(int id)
    {
        GameObject textObject = new GameObject(String.Format("Player{0} Text", id), typeof(TextMeshProUGUI));
        unitCounts.Add(id, textObject.GetComponent<TextMeshProUGUI>());

        textObject.transform.SetParent(infoPanel.transform);
    }


    public void SetPlayerInfo(int id, string text)
    {
        if(unitCounts.TryGetValue(id, out TextMeshProUGUI textLabel))
        { 
            if(textLabel.text != text)
            {
                textLabel.text = text;
            } 
        }
    }
}
