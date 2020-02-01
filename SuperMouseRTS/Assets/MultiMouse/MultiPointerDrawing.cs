using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiPointerDrawing : MonoBehaviour
{
    public Sprite[] cursors;

    private Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

    void Update()
    {
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(300, 20, 200, 50), "System cursor (" + Input.mousePosition.x + ", " + Input.mousePosition.y + ")");

        // TODO: Draw cursors sprites

        //foreach (var pointer in MultiMouse.Instance.GetMousePointers())
        //{
        //}
    }
}
