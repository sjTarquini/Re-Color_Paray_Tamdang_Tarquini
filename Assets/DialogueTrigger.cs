using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Sprites that belong to this sequence. Only one sprite is active at a time.")]
    [SerializeField] private GameObject[] sprites = new GameObject[0];

    [Tooltip("Index of the sprite shown when the scene starts.")]
    [SerializeField] private int startIndex = 0;

    private int currentIndex = -1;

    private void Awake()
    {
        if (sprites == null)
            sprites = new GameObject[0];

        SetAllSpritesActive(false);
        ShowSpriteAt(startIndex);
    }

    public void ShowSpriteAt(int index)
    {
        if (sprites == null || sprites.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, sprites.Length - 1);
        if (index == currentIndex)
            return;

        SetAllSpritesActive(false);

        if (sprites[index] != null)
            sprites[index].SetActive(true);

        currentIndex = index;
    }

    public void ShowNextSprite()
    {
        ShowSpriteAt(currentIndex + 1);
    }

    private void SetAllSpritesActive(bool active)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].SetActive(active);
        }
    }
}
