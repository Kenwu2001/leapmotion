using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightMiddleTip : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public bool isRightMiddleTipTouched => touchCount > 0;

    private int touchCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount++;
                break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount = Mathf.Max(0, touchCount - 1);
                break;
            }
        }
    }
}